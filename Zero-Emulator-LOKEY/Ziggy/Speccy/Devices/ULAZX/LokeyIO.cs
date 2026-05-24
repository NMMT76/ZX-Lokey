using Peripherals;
using Speccy;
using SpeccyCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using LUT = LOKEY.LOKEY_Utility_Types;

namespace LOKEY
{
	public partial class LokeyIO : IODevice
	{
		private bool _debugmode = false;

		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

		private readonly Random _rng = new Random(321);

		private readonly object _lock = new object();

		private zx_spectrum _host = null;

		private Queue<byte> _outqueue = new Queue<byte>();
		private Queue<byte> _inqueue = new Queue<byte>();
		public bool Responded { get; set; }

		//ZXIT RAM
		readonly byte[] _zxitram = new byte[64 * 1024]; //64KB

		//2 Cart ROM banks - Size limit is 64KB so it conforms to overall 16b addressing
		//Given the arrays are 64KB and zero filled, reads in the address space of loaded ROM
		//will return "ROM content", else zero
		//This keeps it "orthogonal" with ZX RAM and SLOW RAM access
		readonly byte[][] _rombanks = new byte[2][];

		private LokeyDiskDrive _diskdrive=new LokeyDiskDrive();

		//Temp RAM to help on copy operation, would be 0 in a real machine to save costs, just implement a smarter memcopy algorithm
		readonly byte[] _tempram = new byte[64 * 1024]; //64KB, allows

		private ELOKEY_ERRORCODE _errorcode = ELOKEY_ERRORCODE.NONE;

		public SPECTRUM_DEVICE DeviceID { get { return SPECTRUM_DEVICE.LOKEY_IO; } }

		private const int _dataport = 137;
		private const int _commandport = 139;

		public LokeyIO()
		{
			// Setup rom "memory"
			for (int c = 0; c < 2; c++)
			{
				//Each rom is 64KB maximum. If the "Loaded ROM" is smaller, slack is zero filled. 
				_rombanks[c]=new byte[1024 * 64];
			}
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
			for (int c = 0; c < 2; c++)
			{
				string filename = LokeyGlobals.LokeyController.GetRomFile(c);
				if (!string.IsNullOrWhiteSpace(filename))
				{
					LoadRom(c, filename);
				}
			}
			_debugmode = true;
			LokeyGlobals.LokeyController.LoadRom = LoadRom;
			LokeyGlobals.LokeyController.LoadDisk = LoadDisk;
			LokeyGlobals.LokeyController.DeviceShow();
			LokeyGlobals.LokeyDebug.DeviceShow();
		}

		public void Out(ushort port, byte val)
		{
			Responded = false;
			if (port == _dataport) //48955 dataport
			{
				_outqueue.Enqueue(val);
				Responded = true;
			}
			else if (port == _commandport) //65339
			{
				VirtualFunctionTable(val);
				Responded = true;
			}
		}
		public byte In(ushort port)
		{
			byte result = 0xff;
			Responded = false;
			if (port == _dataport)
			{
				if (_inqueue.Count > 0)
				{
					result = _inqueue.Dequeue();
				}
				Responded = true;
			}
			else if (port == _commandport)
			{
				Responded = true;
			}
			return result;
		}
		private void VirtualFunctionTable(byte val)
		{
			switch (val)
			{
				case 0: LoadSna(0); break; //ROM0
				case 1: LoadSna(1); break; //ROM1
				case 2: LoadSna(2); break; //DISK

				#region Memory operations
				case 50: MemCopy(); break;
				case 51: MemFill(); break;
				case 52: MemFillRandom(); break;
				#endregion

				case 200: TimeTicks(); break;
				case 250: DebugMode(); break;
				case 251: DebugOut(); break;
			}
		}

		private void LoadSna(int source)
		{
			string srcmethod = $"{nameof(LoadSna)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				byte[] sna = new byte[49179];
				switch (source)
				{
					case 0:
						Array.Copy(_rombanks[0], sna, 49179);
						break;
					case 1:
						Array.Copy(_rombanks[1], sna, 49179);
						break;
					case 2:
						Array.Copy(_diskdrive.GetSna(), sna, 49179);
						break;
				}
				_host.UseSNA(SNAFile.LoadSNA(sna));
			}
		}

		private void MemCopy()
		{
			//ZXIT:TODO - Make it safe(r)...
			string srcmethod = $"{nameof(MemCopy)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 8)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					ArgNumError(srcmethod);
					return;
				}
				byte source = _outqueue.Dequeue();
				byte destination = _outqueue.Dequeue();
				ushort sourcestartaddress = LUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort destinationstartaddress = LUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort length = LUT.ReadUIntegerFromOutQueue(_outqueue);

				IList<byte> sourcebuffer = null;
				IList<byte> destinationbuffer = null;

				switch (source)
				{
					case 0: //ROM0
						sourcebuffer = _rombanks[0];
						break;
					case 1: //ROM1
						sourcebuffer = _rombanks[1];
						break;
					case 2: //Disk Drive
						sourcebuffer = _diskdrive;
						break;
					case 3: //ZX RAM
						sourcebuffer = LokeyGlobals.HostRAM;
						break;
					case 4: //Slow Ram
						sourcebuffer = _zxitram;
						break;
				}
				switch (destination)
				{
					case 2: //Disk Drive
						sourcebuffer = _diskdrive;
						break;
					case 3: //ZX RAM
						destinationbuffer = LokeyGlobals.HostRAM;
						break;
					case 4: //ZX RAM
						destinationbuffer = _zxitram;
						break;
				}
				if (sourcebuffer != null)
				{
					for (int c = 0; c < length; c++)
					{
						_tempram[c] = sourcebuffer[(ushort)(sourcestartaddress + c)];
					}
				}
				else
				{
					for (int c = 0; c < length; c++)
					{
						_tempram[c] = 0;
					}
				}
				if (destinationbuffer != null)
				{
					for (int c = 0; c < length; c++)
					{
						destinationbuffer[(ushort)(destinationstartaddress + c)] = _tempram[c];
					}
				}
			}
		}
		private void MemFill()
		{
			string srcmethod = nameof(MemFill);
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;

				if (_outqueue.Count < 6)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					ArgNumError(srcmethod);
					return;
				}

				byte destination = _outqueue.Dequeue();
				ushort destinationaddress = LUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort length = LUT.ReadUIntegerFromOutQueue(_outqueue);
				byte fillvalue = _outqueue.Dequeue();				

				IList<byte> destinationbuffer = null;

				switch (destination)
				{
					case 2:
						//Disk Drive
						destinationbuffer = _diskdrive;
						break;
					case 3:
						//ZX RAM
						destinationbuffer = LokeyGlobals.HostRAM;
						break;
						//ZXIT RAM
					case 4:
						destinationbuffer = _zxitram;
						break;
				}
				if (destinationbuffer != null)
				{
					for (int c = 0; c < length; c++)
					{
						destinationbuffer[(ushort)(destinationaddress + c)] = fillvalue;
					}
				}
			}
		}
		private void MemFillRandom()
		{
			string srcmethod = nameof(MemFillRandom);

			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;

				if (_outqueue.Count < 7)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					ArgNumError(srcmethod);
					return;
				}

				byte destination = _outqueue.Dequeue();
				ushort destinationaddress = LUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort length = LUT.ReadUIntegerFromOutQueue(_outqueue);
				byte lowerbound = _outqueue.Dequeue();
				byte upperbound = _outqueue.Dequeue();

				IList<byte> destinationbuffer = null;

				switch (destination)
				{
					case 2:
						//Disk Drive
						destinationbuffer = _diskdrive;
						break;
					case 3:
						//ZX RAM
						destinationbuffer = LokeyGlobals.HostRAM;
						break;
					//ZXIT RAM
					case 4:
						destinationbuffer = _zxitram;
						break;
				}
				if (destinationbuffer != null)
				{
					for (int c = 0; c < length; c++)
					{
						destinationbuffer[(ushort)(destinationaddress + c)] = (byte)_rng.Next(lowerbound, upperbound + 1);
					}
				}
			}
		}

		private void SetSpeed(double speedmult)
		{
			_host.SetCPUSpeed(speedmult);
		}

		private void ArgNumError(string origin)
		{
			_errorcode = ELOKEY_ERRORCODE.ARGNUM;
			if (LokeyGlobals.LokeyDebug != null)
			{
				LokeyGlobals.LokeyDebug.DebugOut($"ArgNumError : {origin} : {_outqueue.Count}bytes");
			}
			_outqueue.Clear();
		}
		private void ArgParmError(string origin,string message)
		{
			_errorcode = ELOKEY_ERRORCODE.ARGPARM;
			if (LokeyGlobals.LokeyDebug != null)
			{
				LokeyGlobals.LokeyDebug.DebugOut($"ArgParmError : {origin} : {message}");
			}
			_outqueue.Clear();
		}

		private void DebugMode()
		{
			string srcmethod = $"{nameof(DebugMode)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 1)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				byte type = _outqueue.Dequeue();
				if (type == 0)
				{
					_debugmode = false;
					LokeyGlobals.LokeyDebug.DeviceHide();
				}
				else
				{
					_debugmode = true;
					LokeyGlobals.LokeyDebug.DeviceShow();
				}
			}
		}

		private void DebugOut()
		{
			string srcmethod = $"{nameof(DebugOut)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 3)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				byte type = _outqueue.Dequeue();
				ushort address = LUT.ReadUIntegerFromOutQueue(_outqueue);
				if (!_debugmode) { return; }
				switch (type)
				{
					case 0: //Byte
						LokeyGlobals.LokeyDebug.DebugOut($"BY : {LokeyGlobals.HostRAM[address]}" + System.Environment.NewLine);
						break;
					case 1: //UInteger
						LokeyGlobals.LokeyDebug.DebugOut($"UI : {LUT.ReadUIntegerFromMemory(LokeyGlobals.HostRAM, address)}" + System.Environment.NewLine);
						break;
					case 2: //ULong
						LokeyGlobals.LokeyDebug.DebugOut($"UL : {LUT.ReadULongFromMemory(LokeyGlobals.HostRAM, address)}" + System.Environment.NewLine);
						break;
					case 3: //Float
						LokeyGlobals.LokeyDebug.DebugOut($"FL : {LUT.ReadZXFloatFromMemory(LokeyGlobals.HostRAM, address)}" + System.Environment.NewLine);
						break;
					case 4: //String
						LokeyGlobals.LokeyDebug.DebugOut($"ST : {LUT.ReadStringFromMemory(LokeyGlobals.HostRAM, address)}" + System.Environment.NewLine);
						break;
				}
			}
		}
		private bool LoadRom(int bank, string file)
		{
			if (File.Exists(file))
			{
				long length = 0;
				byte[] filebytes = null;
				using (FileStream fs = File.OpenRead(file))
				{
					if (fs != null)
					{
						length = fs.Length;
						if (length > 0 && length<=64*1024) //64KB Limit on ROM
						{
							filebytes= new byte[length];
							fs.Read(filebytes, 0, (int)length);
						}
					}
				}
				if (length != 0)
				{
					_rombanks[bank] = filebytes;
					return true;
				}
				else
				{
					return false;
				}
			}
			return false;
		}
		private bool LoadDisk(string file)
		{
			return _diskdrive.Load(file);
		}
		private void TimeTicks()
		{
			lock (_lock)
			{
				UInt32 elap = (UInt32)(_stopwatch.ElapsedTicks);
				LUT.WriteULongToInQueue(_inqueue, elap);
			}
		}

		public void Reset()
		{
		}
		public void RegisterDevice(zx_spectrum hostmachine)
		{
			hostmachine.io_devices.Remove(this);
			hostmachine.io_devices.Add(this);
			_host = hostmachine;
			LokeyGlobals.HostRAM = new ZxRam(hostmachine);
		}
		public void UnregisterDevice(zx_spectrum hostmachine)
		{
			hostmachine.io_devices.Remove(this);
			_host = null;
		}

		public void Shutdown()
		{
			LokeyGlobals.LokeyDebug.DeviceHide();
		}
	}
}

