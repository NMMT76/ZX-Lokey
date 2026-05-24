//using Cpu;
//using DirectShowLib;
//using DirectShowLib.BDA;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Operations;
//using NCalc;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Peripherals;
//using Speccy;
//using Speccy.Devices.ULAZX;
//using SpeccyCommon;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Printing;
//using System.IO;
//using System.Linq;
//using System.Numerics;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web.UI.WebControls.WebParts;
//using System.Windows.Forms;
//using System.Windows.Interop;
//using System.Windows.Media.Media3D;
//using Buffer = System.Buffer;
//using ZUT = LOKEY.LOKEY_Utility_Types;

//namespace LOKEY
//{
//	public partial class LOKEY_ULA_ADV : IODevice
//	{
//		private static ILokeyControllerDevice _ulazxcontroller;

//		private static ILokeyDebugDevice _debugdevice;
//		private bool _debugmode = false;

//		private const ushort ZXVAR_NMIADD = 23728;
//		private const ushort ZXVAR_DEST = 23629;
//		private const ushort ZXVAR_WORKSP = 23649;
//		private const ushort ZXVAR_ATTR_P = 23693;

//		private const ushort ATTR_P_VM0 = 23693;
//		private ushort _attr_p_vm1_I = 23728;
//		private ushort _attr_p_vm1_P = 23729;

//		private static Random _rng = new Random(321);

//		private int tr, tb, tg;
//		private readonly object _lock = new object();

//		private zx_spectrum _host = null;
//		private ZxRam _hostram = null;

//		private Queue<byte> _outqueue = new Queue<byte>();
//		private Queue<byte> _inqueue = new Queue<byte>();
//		public bool Responded { get; set; }

//		//8c+8c ZX Original Palette, 32c YEET32 (modded), 16c PICO-8 (modded)
//		private static byte[,] _palette0 = new byte[64, 3]
//		{
//			{0,0,0},{0,0,216},{216,0,0},{216,0,216},{0,216,0},{0,216,216},{216,216,0},{216,216,216},
//			{0,0,0},{0,0,255},{255,0,0},{255,0,255},{0,255,0},{0,255,255},{255,255,0},{255,255,255},
//			{36,36,36},{73,73,73},{128,128,128},{146,146,146},{182,182,182},{143,0,24},{184,0,0},{255,65,0},
//			{255,111,0},{255,170,0},{255,255,156},{4,0,33},{29,24,64},{54,37,84},{100,62,112},{132,99,143},
//			{167,147,173},{224,223,191},{255,255,217},{4,0,68},{57,0,164},{77,154,255},{115,241,255},{39,0,66},
//			{87,0,87},{158,0,126},{214,0,104},{0,64,21},{48,120,0},{86,143,0},{158,207,0},{51,0,6}
//			,{92,16,6},{143,49,20},{161,91,26},{29,43,83},{126,37,83},{0,135,81},{171,82,54},{95,87,79},
//			{255,0,77},{255,163,0},{255,236,39},{0,228,54},{41,173,255},{131,118,156},{255,119,168},{255,204,170}
//		};
//		public List<byte[,]> Palettes { get; private set; } = new List<byte[,]>();

//		//private static byte[,] _palette0 = new byte[16, 3]
//		//{
//		//	{0x00,0x00,0x00},{0x00,0x00,0xff},{0x80,0x00,0x00},{0x80,0x00,0x80},
//		//	{0x00,0x80,0x00},{0x00,0xb2,0xb2},{0xff,0x80,0x00},{0xd8,0xd8,0xd8},
//		//	{0x80,0x80,0x80},{0x00,0x80,0xfa},{0xff,0x00,0x00},{0xff,0x00,0xff},
//		//	{0x00,0xff,0x00},{0x00,0xff,0xff},{0xff,0xff,0x00},{0xff,0xff,0xff},
//		//};

//		//BackBuffers are implemented using whole bytes for convenience, real hardware
//		//would use bitmaps for efficiency.

//		//Holds the rendered image data
//		private readonly byte[] _renderbuffer = new byte[256 * 192]; //6KB

//		//Holds the intermediary result of operations before they are copied to _bitmapbuffer
//		private readonly byte[] _fillbuffer = new byte[256 * 192]; //6KB

//		//Holds the 'sprite' data
//		private readonly byte[] _spritebuffer = new byte[256 * 192]; //6KB

//		//Scratch buffer, enough to hold 256*32 for vertical rolls of 32px
//		private readonly byte[] _scratchbuffer = new byte[256 * 32]; //1KB, much like other buffers, we using bytes instead of bits for convenience

//		//The ULAZX video mode
//		//0 - Use standard ZX Spectrum mode - Default
//		//1 - 4*4 attribute blocks with 64c extended palette
//		public byte VideoMode { get; private set; }

//		//Holds the attribute data
//		private byte _attrblockw = 8;
//		private byte _attrblockh = 8;
//		private byte _attrblockstride = 32;
		
//		private byte[] _attr_renderbuffer_ink = new byte[32 * 24]; //768b 256/8,192/8
//		private byte[] _attr_renderbuffer_paper = new byte[32 * 24]; //768b 256/8,192/8

//		private byte[] _attr_shadowbuffer_paper = new byte[32 * 24]; //768b 256/8,192/8
//		private byte[] _attr_shadowbuffer_ink = new byte[32 * 24]; //768b 256/8,192/8

//		private byte[] _attr_spritebuffer_ink = new byte[32 * 24]; //768b 256/8,192/8
//		private byte[] _attr_spritebuffer_paper = new byte[32 * 24]; //768b 256/8,192/8

//		//ZXIT RAM
//		readonly byte[] _zxitram = new byte[64 * 1024]; //64KB

//		//Rom Font Buffer
//		readonly byte[] _systemromfont = new byte[2048 * 8]; //2KB
//		readonly byte[] _userromfont = new byte[2048 * 8]; //2KB

//		//Color "bias", added to basecolor at pixel render time
//		private int _bias = 0;

//		//2 Cart ROM banks - Size limit is 64KB so it conforms to overall 16b addressing
//		//Given the arrays are 64KB and zero filled, reads in the address space of loaded ROM
//		//will return "ROM content", else zero
//		//This keeps it "orthogonal" with ZX RAM and SLOW RAM access
//		readonly byte[][] _rombanks = new byte[2][];

//		private LokeyDiskDrive _diskdrive=new LokeyDiskDrive();

//		public const byte ULAZX_FILLOVERDRAW = 0x80;
//		public const byte ULAZX_FILLPATTERN = 0x40;
//		public const byte ULAZX_FILLPATTERNINDEX = 0x3f;
//		public const byte ULAZX_FILLPATTERNINDEXY = 0x38;
//		public const byte ULAZX_FILLPATTERNINDEXX = 0x7;

//		//Temp RAM to help on copy operation, would be 0 in a real machine to save costs, just implement a smarter memcopy algorithm
//		readonly byte[] _tempram = new byte[64 * 1024]; //64KB, allows 

//		//Error codes
//		public const byte ULAZXEC_NONE = 0;
//		public const byte ULAZXEC_ARGNUM = 1;

//		private readonly Stopwatch _ulazxstopwatch = Stopwatch.StartNew();

//		private byte[] _rndbytes = new byte[4];

//		private ELOKEY_ERRORCODE _errorcode = ELOKEY_ERRORCODE.NONE;

//		public SPECTRUM_DEVICE DeviceID { get { return SPECTRUM_DEVICE.ULA_ZX; } }

//		private const int _dataport = 63;//63
//		private const int _commandport = 191;//191

//		public LOKEY_ULA_ADV()
//		{
//			// Setup rom "memory"
//			for (int c = 0; c < 2; c++)
//			{
//				//Each rom is 64KB maximum. If the "Loaded ROM" is smaller, slack is zero filled. 
//				_rombanks[c]=new byte[1024 * 64];
//			}

//			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
//			_debugdevice= LokeyGlobals.LokeyDebug;
//			_ulazxcontroller = LokeyGlobals.LokeyController;
//			if (_ulazxcontroller == null)
//			{
//				_ulazxcontroller = new LokeyControllerDevice();
//			}
//			else
//			{
//				for (int c = 0; c < 2; c++)
//				{
//					string filename=_ulazxcontroller.GetRomFile(c);
//					if (!string.IsNullOrWhiteSpace(filename))
//					{
//						LoadRom(c, filename);
//					}
//				}
//			}
//			_debugmode = true;
//			_ulazxcontroller.LoadRom = LoadRom;
//			_ulazxcontroller.LoadDisk = LoadDisk;
//			_ulazxcontroller.DeviceShow();
//			_debugdevice.DeviceShow();
//			Palettes.Add(_palette0);
//			LoadFontSystemRom(_systemromfont, "E:\\Dev\\Data\\ZXData\\zxulagxsystemromfont.bin");
//			LoadFontUserRom(_userromfont, "E:\\Dev\\Data\\ZXData\\zxulagxuserromfont.bin");
//			ResetPatterns();
//			ClearAttributes(true);
//		}

//		public void Out(ushort port, byte val)
//		{
//			Responded = false;
//			if (port == _dataport) //48955 dataport
//			{
//				_outqueue.Enqueue(val);
//				Responded = true;
//			}
//			else if (port == _commandport) //65339
//			{
//				VirtualFunctionTable(val);
//				Responded = true;
//			}
//		}
//		public byte In(ushort port)
//		{
//			byte result = 0xff;
//			Responded = false;
//			if (port == _dataport)
//			{
//				if (_inqueue.Count > 0)
//				{
//					result = _inqueue.Dequeue();
//				}
//				Responded = true;
//			}
//			else if (port == _commandport)
//			{
//				Responded = true;
//			}
//			return result;
//		}
//		private void VirtualFunctionTable(byte val)
//		{
//			switch (val)
//			{
//				case 0: LoadSna(0); break; //ROM0
//				case 1: LoadSna(1); break; //ROM1
//				case 2: LoadSna(2); break; //DISK
//				case 3: SetSpeed(1); break; //Normal Speed //ZXIT:TODO Callback
//				case 4: SetSpeed(1.25); break; //Turbo Speed //ZXIT:TODO Callback

//				case 5: SetVideoMode0(); break;
//				case 6: SetVideoMode1(); break;
//				case 7: SetVideoMode1AttributeAddress(); break;

//				case 10: Cls(); break;
//				case 12: ClearBitmap(false); break;
//				case 13: ClearAttributes(false); break;

//				case 20: Plot(); break;
//				case 22: FloodFill(); break;
//				case 30: Line(); break;
//				case 33: Triangle(false); break; //Triangle
//				case 34: Triangle(true); break; //TriangleFilled
//				case 35: Circle(false); break; //Circle
//				case 36: Circle(true); break; //CircleFilled
//				case 37: Rectangle(false); break; //Rectangle
//				case 38: Rectangle(true); break; //RectangleFilled
				
//				case 50: Print(false,false); break; //PrintSR
//				case 51: Print(false,true); break; //PrintSRMasked
//				case 52: Print(true,false); break; //PrintUR
//				case 53: Print(true,true); break; //PrintURMasked
//				case 54: PrintScreen(false); break; //PrintScreenSR
//				case 55: PrintScreen(true); break; //PrintScreenUR
				
//				case 101: BiasSetPositive(); break;
//				case 102: BiasSetNegative(); break;
				
//				case 110: CopyToSprite(); break;
//				case 111: CopySpriteToRender(); break;
//				case 112: LoadScreenToBuffer(_renderbuffer, _attr_renderbuffer_ink, _attr_renderbuffer_paper); break; //To Render buffer
//				case 113: LoadScreenToBuffer(_spritebuffer, _attr_spritebuffer_ink, _attr_spritebuffer_paper); break; //To Sprite buffer
				
//				case 120: Blit(); break; //Blit
//				case 121: BlitAttribute(); break; //Blit

//				case 130: RndBytes(); break;
//				case 131: RndBytesLimited(); break;

//				case 140: Scroll(); break;
//				case 141: Roll(); break;
				
//				#region Memory operations
//				case 180: MemCopy(); break;
//				case 181: MemFill(); break;
//				case 182: MemRandomFill(); break;
//				#endregion

//				case 200: TimeTicks(); break;
//				case 240: EvaluateExpression(); break;
//				case 250: DebugMode(); break;
//				case 251: DebugOut(); break;
//				case 255: RenderStack(); break;
//			}
//		}

//		private void SetSpeed(double speedmult)
//		{
//			_host.SetCPUSpeed(speedmult);
//		}

//		private void LoadSna(int source)
//		{
//			string srcmethod = $"{nameof(LoadSna)}";
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				byte[] sna = new byte[49179];
//				switch (source)
//				{
//					case 0:
//						Array.Copy(_rombanks[0], sna, 49179);
//						break;
//					case 1:
//						Array.Copy(_rombanks[1], sna, 49179);
//						break;
//					case 2:
//						Array.Copy(_diskdrive.GetSna(), sna, 49179);
//						break;
//				}
//				_host.UseSNA(SNAFile.LoadSNA(sna));
//			}
//		}

//		private void MemCopy()
//		{
//			//ZXIT:TODO - Make it safe(r)...
//			string srcmethod = $"{nameof(MemCopy)}";
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 8)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					ArgNumError(srcmethod);
//					return;
//				}
//				byte source = _outqueue.Dequeue();
//				byte destination = _outqueue.Dequeue();
//				ushort sourcestartaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				ushort destinationstartaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				ushort length = ZUT.ReadUIntegerFromOutQueue(_outqueue);

//				IList<byte> sourcebuffer = null;
//				IList<byte> destinationbuffer = null;

//				switch (source)
//				{
//					case 0: //ROM0
//						sourcebuffer = _rombanks[0];
//						break;
//					case 1: //ROM1
//						sourcebuffer = _rombanks[1];
//						break;
//					case 2: //Disk Drive
//						sourcebuffer = _diskdrive;
//						break;
//					case 3: //ZX RAM
//						sourcebuffer = _hostram;
//						break;
//					case 4: //Slow Ram
//						sourcebuffer = _zxitram;
//						break;
//				}
//				switch (destination)
//				{
//					case 2: //Disk Drive
//						sourcebuffer = _diskdrive;
//						break;
//					case 3: //ZX RAM
//						destinationbuffer = _hostram;
//						break;
//					case 4: //ZX RAM
//						destinationbuffer = _hostram;
//						break;
//				}
//				if (sourcebuffer != null)
//				{
//					for (int c = 0; c < length; c++)
//					{
//						_tempram[c] = sourcebuffer[(ushort)(sourcestartaddress + c)];
//					}
//				}
//				else
//				{
//					for (int c = 0; c < length; c++)
//					{
//						_tempram[c] = 0;
//					}
//				}
//				if (destinationbuffer != null)
//				{
//					for (int c = 0; c < length; c++)
//					{
//						destinationbuffer[(ushort)(destinationstartaddress + c)] = _tempram[c];
//					}
//				}
//			}
//		}
//		private void MemFill()
//		{
//			string srcmethod = nameof(MemFill);
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;

//				if (_outqueue.Count < 6)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					ArgNumError(srcmethod);
//					return;
//				}

//				byte destination = _outqueue.Dequeue();
//				ushort destinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				byte fillvalue = _outqueue.Dequeue();
//				ushort length = ZUT.ReadUIntegerFromOutQueue(_outqueue);

//				IList<byte> destinationbuffer = null;

//				switch (destination)
//				{
//					case 2:
//						//Disk Drive
//						destinationbuffer = _diskdrive;
//						break;
//					case 3:
//						//ZX RAM
//						destinationbuffer = _hostram;
//						break;
//						//ZXIT RAM
//					case 4:
//						destinationbuffer = _zxitram;
//						break;
//				}
//				if (destinationbuffer != null)
//				{
//					for (int c = 0; c < length; c++)
//					{
//						destinationbuffer[(ushort)(destinationaddress + c)] = fillvalue;
//					}
//				}
//			}
//		}
//		private void MemRandomFill()
//		{
//			string srcmethod = nameof(MemRandomFill);

//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;

//				if (_outqueue.Count < 7)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					ArgNumError(srcmethod);
//					return;
//				}

//				byte destination = _outqueue.Dequeue();
//				ushort destinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				byte lowerbound = _outqueue.Dequeue();
//				byte upperbound = _outqueue.Dequeue();
//				ushort length = ZUT.ReadUIntegerFromOutQueue(_outqueue);

//				IList<byte> destinationbuffer = null;

//				switch (destination)
//				{
//					case 2:
//						//Disk Drive
//						destinationbuffer = _diskdrive;
//						break;
//					case 3:
//						//ZX RAM
//						destinationbuffer = _hostram;
//						break;
//					//ZXIT RAM
//					case 4:
//						destinationbuffer = _zxitram;
//						break;
//				}
//				if (destinationbuffer != null)
//				{
//					for (int c = 0; c < length; c++)
//					{
//						destinationbuffer[(ushort)(destinationaddress + c)] = (byte)_rng.Next(lowerbound, upperbound + 1);
//					}
//				}
//			}
//		}

//		public void SetVideoMode0()
//		{
//			lock (_lock)
//			{
//				VideoMode = 0;
//				_attrblockw = 8;
//				_attrblockh = 8;
//				_attrblockstride = 32;
//				_attr_renderbuffer_ink = new byte[32 * 24];
//				_attr_shadowbuffer_ink = new byte[32 * 24];
//				_attr_spritebuffer_ink = new byte[32 * 24];
//				_attr_renderbuffer_paper = new byte[32 * 24];
//				_attr_shadowbuffer_paper = new byte[32 * 24];
//				_attr_spritebuffer_paper = new byte[32 * 24];
//				ClearAttributes(false);
//			}
//		}
//		public void SetVideoMode1()
//		{
//			lock (_lock)
//			{
//				VideoMode = 1;
//				_attrblockw = 4;
//				_attrblockh = 4;
//				_attrblockstride = 64;
//				_attr_renderbuffer_ink = new byte[64 * 48];
//				_attr_shadowbuffer_ink =new byte[64 * 48];
//				_attr_spritebuffer_ink = new byte[64 * 48];
//				_attr_renderbuffer_paper = new byte[64 * 48];
//				_attr_shadowbuffer_paper =new byte[64 * 48];
//				_attr_spritebuffer_paper = new byte[64 * 48];
//				ClearAttributes(false);
//			}
//		}
//		public void SetVideoMode1AttributeAddress()
//		{
//			string srcmethod = $"{nameof(SetVideoMode1AttributeAddress)}";
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 2)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					ArgNumError(srcmethod);
//					return;
//				}
//				byte lsb = _outqueue.Dequeue();
//				byte msb = _outqueue.Dequeue();
//				_attr_p_vm1_I = (ushort)(lsb + msb * 256);
//				_attr_p_vm1_P = (ushort)((lsb + msb * 256) + 1);
//			}
//		}
//		private void RenderStack()
//		{
//			lock (_lock)
//			{
//				byte[] zxbuffer = ZxScreenConversion.LinearPixelmapToZxBitmap(_renderbuffer);
//				for (int c = 0; c < 6144; c++)
//				{
//					_hostram[16384+c]=zxbuffer[c];
//				}
//				if (VideoMode == 0)
//				{
//					for (int c = 0; c < 768; c++)
//					{
//						_hostram[16384 + 6144 + c] = _attr_renderbuffer_ink[c];
//					}
//				}
//				else
//				{
//					for (int c = 0; c < _attr_renderbuffer_ink.Length; c++)
//					{
//						_attr_shadowbuffer_ink[c] = _attr_renderbuffer_ink[c];
//						_attr_shadowbuffer_paper[c] = _attr_renderbuffer_paper[c];
//					}
//				}
//			}
//		}
//		public void SetAttribute(byte x, byte y)
//		{
//			_attr_renderbuffer_ink[(y / _attrblockh) * _attrblockstride + (x / _attrblockw)] = _hostram[_attr_p_vm1_I];
//			_attr_renderbuffer_paper[(y / _attrblockh) * _attrblockstride + (x / _attrblockw)] = _hostram[_attr_p_vm1_P];
//		}
//		public void Plot()
//		{
//			lock(_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 2)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					_debugdevice.DebugOut($"{nameof(Plot)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
//					return;
//				}
//				byte x = _outqueue.Dequeue();
//				byte y = _outqueue.Dequeue();
//				if (x > 255 || y > 191) { return; }
//				_renderbuffer[y * 256 + x] = 1;
//				if (VideoMode == 0)
//				{
//					_attr_renderbuffer_ink[(y / _attrblockh) * _attrblockstride + (x / _attrblockw)] = _hostram[ATTR_P_VM0];
//				}
//				else
//				{
//					SetAttribute(x, y);
//				}
//			}
//		}
//		public void Line()
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 4)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					_debugdevice.DebugOut($"{nameof(Line)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
//					return;
//				}
//				byte x1 = _outqueue.Dequeue();
//				byte y1 = _outqueue.Dequeue();
//				byte x2 = _outqueue.Dequeue();
//				byte y2 = _outqueue.Dequeue();
//				LineBLA(x1, y1, x2, y2, _renderbuffer, _attr_renderbuffer_ink);
//			}
//		}
//		private void LineBLA(byte x1, byte y1, byte x2, byte y2, byte[] bitmapbuffer, byte[] attributebuffer)
//		{
//			if (bitmapbuffer == null) { throw new ArgumentNullException(nameof(bitmapbuffer)); }
//			int ix1 = x1;
//			int iy1 = y1;
//			int ix2 = x2;
//			int iy2 = y2;
//			int dx = Math.Abs(ix2 - ix1);
//			int dy = Math.Abs(iy2 - iy1);
//			int sx = ix1 < ix2 ? 1 : -1;
//			int sy = iy1 < iy2 ? 1 : -1;
//			int err = dx - dy;
//			while (true)
//			{
//				if ((uint)ix1 < 256u && (uint)iy1 < 192u)
//				{
//					bitmapbuffer[iy1 * 256 + ix1] = 1;
//					if (VideoMode == 0)
//					{
//						attributebuffer[((iy1) / _attrblockh) * _attrblockstride + (ix1 / _attrblockw)] = _hostram[ATTR_P_VM0];
//					}
//					else
//					{
//						SetAttribute((byte)ix1, (byte)iy1);
//					}
//				}
//				if (ix1 == ix2 && iy1 == iy2) { break; }
//				int e2 = err << 1;
//				if (e2 > -dy)
//				{
//					err -= dy;
//					ix1 += sx;
//				}
//				if (e2 < dx)
//				{
//					err += dx;
//					iy1 += sy;
//				}
//			}
//		}
//		private void Rectangle(bool filled)
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (filled)
//				{
//					if (_outqueue.Count < 5)
//					{
//						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//						_debugdevice.DebugOut($"{nameof(Rectangle)} (Filled) : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
//						return;
//					}
//				}
//				else
//				{
//					if (_outqueue.Count < 4)
//					{
//						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//						_debugdevice.DebugOut($"{nameof(Rectangle)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
//						return;
//					}
//				}

//				byte x1 = _outqueue.Dequeue();
//				byte y1 = _outqueue.Dequeue();
//				byte x2 = _outqueue.Dequeue();
//				byte y2 = _outqueue.Dequeue();
//				byte fillbitpattern = 0;
//				byte filloverdraw = 0;
//				byte fillpattern = 0;
//				byte fillindex = 0;
//				byte swap;
//				if (filled)
//				{
//					fillbitpattern = _outqueue.Dequeue();
//					if (fillbitpattern != 0)
//					{
//						filloverdraw = (byte)((fillbitpattern & ULAZX_FILLOVERDRAW) >> 7);
//						fillpattern = (byte)((fillbitpattern & ULAZX_FILLPATTERN) >> 6);
//						fillindex = (byte)(fillbitpattern & ULAZX_FILLPATTERNINDEX);
//					}
//				}
//				if (x1 == x2 && y1 == y2 && y1 < 192)
//				{
//					_renderbuffer[y1 * 256 + x1] = 1;
//					if (VideoMode == 0)
//					{
//						_attr_renderbuffer_ink[(y1 / _attrblockh) * _attrblockstride + (x1 / _attrblockw)] = _hostram[ATTR_P_VM0];
//					}
//					else
//					{
//						SetAttribute(x1, y1);
//					}
//					return;
//				}
//				else
//				{
//					//Order the points so that x1<x2 and y1<y2 to simplify operations
//					if (x1 > x2) { swap = x1; x1 = x2; x2 = swap; }
//					if (y1 > y2) { swap = y1; y1 = y2; y2 = swap; }

//					ClearPolyBuffer();
//					LineBLA(x1, y1, x2, y1, _fillbuffer, _attr_renderbuffer_ink);
//					LineBLA(x2, y1, x2, y2, _fillbuffer, _attr_renderbuffer_ink);
//					LineBLA(x2, y2, x1, y2, _fillbuffer, _attr_renderbuffer_ink);
//					LineBLA(x1, y2, x1, y1, _fillbuffer, _attr_renderbuffer_ink);
//					if (filled)
//					{
//						//Rectangle degeneracy check
//						if (!((x2 - x1) < 2 || (y2 - y1) < 2))
//						{
//							//On rectangles we don't need the centroid, x1+1,y1+1 solves all cases
//							ScanlineFloodFill((byte)(x1 + 1), (byte)(y1 + 1), _fillbuffer);
//						}
//					}
//					if (fillpattern == 1)
//					{
//						PatternFill(fillindex, _fillbuffer);
//					}
//					if (filloverdraw == 1)
//					{
//						LineBLA(x1, y1, x2, y1, _fillbuffer, _attr_renderbuffer_ink);
//						LineBLA(x2, y1, x2, y2, _fillbuffer, _attr_renderbuffer_ink);
//						LineBLA(x2, y2, x1, y2, _fillbuffer, _attr_renderbuffer_ink);
//						LineBLA(x1, y2, x1, y1, _fillbuffer, _attr_renderbuffer_ink);
//					}
//					CopyPolyToBack();
//				}
//			}
//		}
		
//		public void Circle(bool filled)
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 3) { _errorcode = ELOKEY_ERRORCODE.ARGNUM; return; }
//				byte x = _outqueue.Dequeue();
//				byte y = _outqueue.Dequeue();
//				byte r = _outqueue.Dequeue();
//				byte fillbitpattern = 0;
//				byte filloverdraw = 0;
//				byte fillpattern = 0;
//				byte fillindex = 0;
//				if (filled)
//				{
//					fillbitpattern = _outqueue.Dequeue();
//					if (fillbitpattern != 0)
//					{
//						filloverdraw = (byte)((fillbitpattern & ULAZX_FILLOVERDRAW) >> 7);
//						fillpattern = (byte)((fillbitpattern & ULAZX_FILLPATTERN) >> 6);
//						fillindex = (byte)(fillbitpattern & ULAZX_FILLPATTERNINDEX);
//					}
//				}
//				if (r == 0) { return; }
//				if (r == 1)
//				{
//					if (y < 192)
//					{
//						_renderbuffer[y * 256 + x] = 1;
//						if (VideoMode == 0)
//						{
//							_attr_renderbuffer_ink[(y / _attrblockh) + _attrblockstride + (x / _attrblockw)] = _hostram[ATTR_P_VM0];
//						}
//						else
//						{
//							SetAttribute(x, y);
//						}
//					}
//					return;
//				}
//				ClearPolyBuffer();
//				CircleMCA(x, y, r, _fillbuffer);
//				if (filled)
//				{
//					ScanlineFloodFill(x, y, _fillbuffer);
//				}
//				if (fillpattern == 1)
//				{
//					PatternFill(fillindex, _fillbuffer);
//				}
//				if (filloverdraw == 1)
//				{
//					CircleMCA(x, y, r, _fillbuffer);
//				}
//				CopyPolyToBack();
//			}
//		}
//		public void CircleMCA(byte x, byte y, byte r, byte[] buffer)
//		{
//			long work = 0;
//			int cx = x;
//			int cy = y;
//			int radius = r;

//			int dx = radius;
//			int dy = 0;
//			int decision = 1 - radius;

//			while (dx >= dy)
//			{
//				SetCirclePoint(cx + dx, cy + dy, buffer); work++;
//				SetCirclePoint(cx + dy, cy + dx, buffer); work++;
//				SetCirclePoint(cx - dy, cy + dx, buffer); work++;
//				SetCirclePoint(cx - dx, cy + dy, buffer); work++;
//				SetCirclePoint(cx - dx, cy - dy, buffer); work++;
//				SetCirclePoint(cx - dy, cy - dx, buffer); work++;
//				SetCirclePoint(cx + dy, cy - dx, buffer); work++;
//				SetCirclePoint(cx + dx, cy - dy, buffer); work++;

//				dy++;

//				if (decision <= 0)
//				{
//					decision += (2 * dy) + 1;
//				}
//				else
//				{
//					dx--;
//					decision += (2 * (dy - dx)) + 1;
//				}
//			}
//		}

//		private void SetCirclePoint(int x, int y, byte[] buffer)
//		{
//			if ((uint)x >= 256u || (uint)y >= 192u) { return; }
//			buffer[y * 256 + x] = 1;
//			if (VideoMode == 0)
//			{
//				_attr_renderbuffer_ink[(y / _attrblockh) * _attrblockstride + (x / _attrblockw)] = _hostram[ATTR_P_VM0];
//			}
//			else
//			{
//				SetAttribute((byte)x,(byte)y);
//			}
//		}

//		private void Triangle(bool filled)
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (filled)
//				{
//					if (_outqueue.Count < 7)
//					{
//						_debugdevice.DebugOut($"{nameof(Triangle)} (Filled) : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
//						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//						return;
//					}
//				}
//				else
//				{
//					if (_outqueue.Count < 6)
//					{
//						_debugdevice.DebugOut($"{nameof(Triangle)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
//						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//						return;
//					}
//				}
//				List<PointByte2D> points = new List<PointByte2D>();
//				byte fillbitpattern = 0;
//				byte filloverdraw = 0;
//				byte fillpattern = 0;
//				byte fillindex = 0;
//				for (int c = 0; c < 3; c++)
//				{
//					PointByte2D temp = new PointByte2D(_outqueue.Dequeue(), _outqueue.Dequeue());
//					points.Add(temp);
//				}
//				if (filled)
//				{
//					fillbitpattern = _outqueue.Dequeue();
//					if (fillbitpattern != 0)
//					{
//						filloverdraw = (byte)((fillbitpattern & ULAZX_FILLOVERDRAW) >> 7);
//						fillpattern = (byte)((fillbitpattern & ULAZX_FILLPATTERN) >> 6);
//						fillindex = (byte)(fillbitpattern & ULAZX_FILLPATTERNINDEX);
//					}
//				}
//				if (!filled)
//				{
//					LineBLA(points[0].X, points[0].Y, points[1].X, points[1].Y, _renderbuffer, _attr_renderbuffer_ink);
//					LineBLA(points[1].X, points[1].Y, points[2].X, points[2].Y, _renderbuffer, _attr_renderbuffer_ink);
//					LineBLA(points[2].X, points[2].Y, points[0].X, points[0].Y, _renderbuffer, _attr_renderbuffer_ink);
//				}
//				else
//				{
//					ClearPolyBuffer();
//					LineBLA(points[0].X, points[0].Y, points[1].X, points[1].Y, _fillbuffer, _attr_renderbuffer_ink);
//					LineBLA(points[1].X, points[1].Y, points[2].X, points[2].Y, _fillbuffer, _attr_renderbuffer_ink);
//					LineBLA(points[2].X, points[2].Y, points[0].X, points[0].Y, _fillbuffer, _attr_renderbuffer_ink);
//					if (filled)
//					{
//						//Check for degeneracy before fill
//						if (!IsDegenerateTriangle(points))
//						{
//							double xc, yc;
//							//Centroid of a triangle (specific version)
//							xc = (points[0].X + points[1].X + points[2].X) / 3.0;
//							yc = (points[0].Y + points[1].Y + points[2].Y) / 3.0;
//							ScanlineFloodFill((byte)xc, (byte)yc, _fillbuffer);
//						}
//					}
//					if (fillpattern == 1)
//					{
//						PatternFill(fillindex, _fillbuffer);
//					}
//					if (filloverdraw == 1)
//					{
//						LineBLA(points[0].X, points[0].Y, points[1].X, points[1].Y, _fillbuffer, _attr_renderbuffer_ink);
//						LineBLA(points[1].X, points[1].Y, points[2].X, points[2].Y, _fillbuffer, _attr_renderbuffer_ink);
//						LineBLA(points[2].X, points[2].Y, points[0].X, points[0].Y, _fillbuffer, _attr_renderbuffer_ink);
//					}
//					CopyPolyToBack();
//				}
//			}
//		}
//		public static bool IsDegenerateTriangle(List<PointByte2D> points)
//		{
//			int areaTwice = points[0].X * (points[1].Y-points[2].Y)
//						  + points[1].X * (points[2].Y - points[0].Y)
//						  + points[2].X * (points[0].Y - points[1].Y);
//			return areaTwice == 0;
//		}
//		public static long TriangleArea(int x1, int y1, int x2, int y2, int x3, int y3)
//		{
//			return (long)(Math.Abs((x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2)) / 2.0));
//		}

//		private void CopyToSprite()
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 2) { _errorcode = ELOKEY_ERRORCODE.ARGNUM; return; }
//				byte source = _outqueue.Dequeue();
//				ushort sourceaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);

//				byte[] _source = null;

//				switch (source)
//				{
//					case 0: //ZX RAM

//						break;
//				}

//				int index = 0;
//				int spriteaddr = 0;
//				byte temp = 0;
//				while (index < 8192)
//				{
//					if (sourceaddress + index < (64 * 1024))
//					{
//						temp = _rombanks[source][sourceaddress + index];
//					}
//					else
//					{
//						temp = 0;
//					}
//					//ZXIT:TODO - Do it properly, not this lazy ish...
//					string tempbin = Convert.ToString(temp, 2).PadLeft(8, '0');
//					for (int c = 0; c < 8; c++)
//					{
//						if (tempbin[c] == '0')
//						{
//							_spritebuffer[spriteaddr] = 0;
//						}
//						else
//						{
//							_spritebuffer[spriteaddr] = 1;
//						}
//						spriteaddr++;
//					}
//					index++;
//				}
//			}
//		}
//		private void ArgNumError(string origin)
//		{
//			_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//			if (_debugdevice != null)
//			{
//				_debugdevice.DebugOut($"ArgNumError : {origin} : {_outqueue.Count}bytes");
//			}
//			_outqueue.Clear();
//		}
//		private void ArgParmError(string origin,string message)
//		{
//			_errorcode = ELOKEY_ERRORCODE.ARGPARM;
//			if (_debugdevice != null)
//			{
//				_debugdevice.DebugOut($"ArgParmError : {origin} : {message}");
//			}
//			_outqueue.Clear();
//		}

//		private void Scroll()
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 3)
//				{
//					if (_outqueue.Count < 3)
//					{
//						ArgNumError($"{nameof(Scroll)}");
//						return;
//					}
//					return;
//				}
//				byte direction = _outqueue.Dequeue();
//				byte numpixels = _outqueue.Dequeue();
//				byte attribs = _outqueue.Dequeue();
//				if (direction == 0) { return; }
//				if (numpixels == 0) { return; }
//				if (numpixels != 1 && numpixels != 2 && numpixels != 4 && numpixels != 8 && numpixels != 16 && numpixels != 32)
//				{
//					numpixels = 1;
//				}
//				if (VideoMode == 0)
//				{
//					if (numpixels == 4) { numpixels = 8; }
//				}
//				TranslateBuffer(_renderbuffer, _scratchbuffer, 256, 192, (ScrollDirection)(direction), numpixels, false);

//				//Skip attributes if numpixels doesnt match attribute block size or a multiple of it
//				if (VideoMode == 0 && (numpixels != 8 && numpixels != 16 && numpixels != 32)) { return; }
//				if (VideoMode == 1 && (numpixels != 4 && numpixels != 8 && numpixels != 16 && numpixels != 32)) { return; }
//				if (attribs != 0)
//				{
//					if (VideoMode == 0)
//					{
//						numpixels = (byte)(numpixels / 8);
//						TranslateBuffer(_attr_renderbuffer_ink, _scratchbuffer, 32, 24, (ScrollDirection)(direction), numpixels, false);
//						TranslateBuffer(_attr_renderbuffer_paper, _scratchbuffer, 32, 24, (ScrollDirection)(direction), numpixels, false);
//					}
//					else
//					{
//						numpixels = (byte)(numpixels / 4);
//						TranslateBuffer(_attr_renderbuffer_ink, _scratchbuffer, 64, 48, (ScrollDirection)(direction), numpixels, false);
//						TranslateBuffer(_attr_renderbuffer_paper, _scratchbuffer, 64, 48, (ScrollDirection)(direction), numpixels, false);
//					}
//				}
//			}
//		}
//		//public void PreciseDelay(Stopwatch stopw, long endTarget)
//		//{
//		//	// Inside your loop
//		//	long remainingTicks = endTarget - stopw.ElapsedTicks;
//		//	double remainingMs = (double)remainingTicks / Stopwatch.Frequency * 1000;
//		//	// Final precision spin
//		//	while (stopw.ElapsedTicks < endTarget)
//		//	{
//		//		Thread.SpinWait(1);
//		//	}
//		//}
//		private void Roll()
//		{
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 3)
//				{
//					ArgNumError($"{nameof(Roll)}");
//					 return;
//				}
//				byte direction = _outqueue.Dequeue();
//				byte numpixels = _outqueue.Dequeue();
//				byte attribs = _outqueue.Dequeue();
//				if (direction == 0) { return; }
//				if (numpixels == 0) { return; }
//				//
//				if (numpixels != 1 && numpixels != 2 && numpixels != 4 && numpixels != 8 && numpixels != 16 && numpixels != 32)
//				{
//					numpixels = 1;
//				}
//				TranslateBuffer(_renderbuffer, _scratchbuffer, 256, 192, (ScrollDirection)(direction), numpixels, true);

//				//Skip attributes if numpixels doesnt match attribute block size or a multiple of it
//				if (VideoMode == 0 && (numpixels != 8 && numpixels != 16 && numpixels != 32)) { return; }
//				if (VideoMode == 1 && (numpixels != 4 && numpixels != 8 && numpixels != 16 && numpixels != 32)) { return; }
//				if (attribs != 0)
//				{
//					if (VideoMode == 0)
//					{
//						numpixels = (byte)(numpixels / 8);
//						TranslateBuffer(_attr_renderbuffer_ink, _scratchbuffer, 32, 24, (ScrollDirection)(direction), numpixels, true);
//						TranslateBuffer(_attr_renderbuffer_paper, _scratchbuffer, 32, 24, (ScrollDirection)(direction), numpixels, true);
//					}
//					else
//					{
//						numpixels = (byte)(numpixels / 4);
//						TranslateBuffer(_attr_renderbuffer_ink, _scratchbuffer, 64, 48, (ScrollDirection)(direction), numpixels, true);
//						TranslateBuffer(_attr_renderbuffer_paper, _scratchbuffer, 64, 48, (ScrollDirection)(direction), numpixels, true);
//					}
//				}
//			}
//		}

//		private static void TranslateBuffer(byte[] buffer,byte[] scratch,int width,int height,ScrollDirection direction,int step,bool wrap)
//		{
//			if (buffer == null || scratch == null)
//				throw new ArgumentNullException();

//			if (buffer.Length != width * height)
//				throw new ArgumentException("Buffer size mismatch.");

//			if (step != 1 && step != 2 && step != 4 && step != 8 &&
//				step != 16 && step != 32)
//				throw new ArgumentException("Invalid step.");

//			// Resolve direction
//			int dx = 0, dy = 0;

//			switch (direction)
//			{
//				case ScrollDirection.Up: dy = -step; break;
//				case ScrollDirection.Down: dy = step; break;
//				case ScrollDirection.Left: dx = -step; break;
//				case ScrollDirection.Right: dx = step; break;

//				case ScrollDirection.UpLeft: dx = -step; dy = -step; break;
//				case ScrollDirection.UpRight: dx = step; dy = -step; break;
//				case ScrollDirection.DownLeft: dx = -step; dy = step; break;
//				case ScrollDirection.DownRight: dx = step; dy = step; break;
//			}

//			if (dx == 0 && dy == 0)
//				return;

//			// Compose (orthogonal)
//			if (dy != 0)
//				Vertical(buffer, scratch, width, height, dy, wrap);

//			if (dx != 0)
//				Horizontal(buffer, scratch, width, height, dx, wrap);
//		}

//		private static void Vertical(byte[] buffer,byte[] scratch,int width,int height,int dy,bool wrap)
//		{
//			int shift = Math.Abs(dy);
//			if (shift >= height)
//			{
//				if (!wrap)
//					Array.Clear(buffer, 0, buffer.Length);
//				return;
//			}

//			int rowSize = width;
//			int blockBytes = shift * rowSize;

//			if (scratch.Length < blockBytes)
//				throw new ArgumentException("Scratch too small for vertical operation.");

//			if (dy < 0) // UP
//			{
//				// Save top rows
//				Buffer.BlockCopy(buffer, 0, scratch, 0, blockBytes);

//				// Move main block up
//				Buffer.BlockCopy(buffer, blockBytes, buffer, 0, (height - shift) * rowSize);

//				if (wrap)
//				{
//					// Restore to bottom
//					Buffer.BlockCopy(scratch, 0, buffer, (height - shift) * rowSize, blockBytes);
//				}
//				else
//				{
//					Array.Clear(buffer, (height - shift) * rowSize, blockBytes);
//				}
//			}
//			else // DOWN
//			{
//				// Save bottom rows
//				int offset = (height - shift) * rowSize;
//				Buffer.BlockCopy(buffer, offset, scratch, 0, blockBytes);

//				// Move main block down
//				Buffer.BlockCopy(buffer, 0, buffer, blockBytes, (height - shift) * rowSize);

//				if (wrap)
//				{
//					Buffer.BlockCopy(scratch, 0, buffer, 0, blockBytes);
//				}
//				else
//				{
//					Array.Clear(buffer, 0, blockBytes);
//				}
//			}
//		}

//		private static void Horizontal(byte[] buffer,byte[] scratch,int width,int height,int dx,bool wrap)
//		{
//			int shift = Math.Abs(dx);
//			if (shift >= width)
//			{
//				if (!wrap)
//					Array.Clear(buffer, 0, buffer.Length);
//				return;
//			}

//			if (scratch.Length < shift)
//				throw new ArgumentException("Scratch too small for horizontal operation.");

//			int move = width - shift;

//			for (int y = 0; y < height; y++)
//			{
//				int row = y * width;

//				if (dx < 0) // LEFT
//				{
//					// Save left edge
//					Buffer.BlockCopy(buffer, row, scratch, 0, shift);

//					// Shift row left
//					Buffer.BlockCopy(buffer, row + shift, buffer, row, move);

//					if (wrap)
//						Buffer.BlockCopy(scratch, 0, buffer, row + move, shift);
//					else
//						Array.Clear(buffer, row + move, shift);
//				}
//				else // RIGHT
//				{
//					// Save right edge
//					Buffer.BlockCopy(buffer, row + move, scratch, 0, shift);

//					// Shift row right
//					Buffer.BlockCopy(buffer, row, buffer, row + shift, move);

//					if (wrap)
//						Buffer.BlockCopy(scratch, 0, buffer, row, shift);
//					else
//						Array.Clear(buffer, row, shift);
//				}
//			}
//		}

//		private void Blit()
//		{
//			string srcmethod = $"{nameof(Blit)}";
//			lock (_lock)
//			{
//				if (_outqueue.Count < 8)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					_debugdevice.DebugOut($"{srcmethod} : ARGNUM count={_outqueue.Count};");
//					_outqueue.Clear();
//					return;
//				}
//				byte sx = _outqueue.Dequeue();
//				byte sy = _outqueue.Dequeue();
//				byte dx = _outqueue.Dequeue();
//				byte dy = _outqueue.Dequeue();
//				int width = ZUT.ReadUIntegerFromOutQueue(_outqueue);//_outqueue.Dequeue();
//				if (width > 256) { width = 256; }
//				int height = _outqueue.Dequeue();
//				if (height > 192) { height = 192; }
//				int sourcedest = _outqueue.Dequeue();
//				byte[] _src;
//				byte[] _dest;
//				if ((sourcedest & 0b10) == 0)
//				{
//					_src = _renderbuffer;
//				}
//				else
//				{
//					_src = _spritebuffer;
//				}
//				if ((sourcedest & 0b01) == 0)
//				{
//					_dest = _renderbuffer;
//				}
//				else
//				{
//					_dest = _spritebuffer;
//				}
//				if ((sx > 256 - width) || (sy > 192 - height) || (dx > 256 - width) || (dy > 192 - height))
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGPARM;
//					_debugdevice.DebugOut($"{srcmethod} : ARGERR");
//					return;
//				}
//				for (int y = 0; y < height; y++)
//				{
//					for (int x = 0; x < width; x++)
//					{
//						_dest[(dy + y) * 256 + (dx + x)] = _src[(sy + y) * 256 + (sx + x)];
//					}
//				}
//			}
//		}
//		private void BlitAttribute()
//		{
//			string srcmethod = $"{nameof(BlitAttribute)}";
//			lock (_lock)
//			{
//				if (_outqueue.Count < 6)
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
//					_debugdevice.DebugOut($"{srcmethod} : ARGNUM count={_outqueue.Count};");
//					_outqueue.Clear();
//					return;
//				}
//				int sx = _outqueue.Dequeue();
//				int sy = _outqueue.Dequeue();
//				int dx = _outqueue.Dequeue();
//				int dy = _outqueue.Dequeue();
//				int width = ZUT.ReadUIntegerFromOutQueue(_outqueue);// _outqueue.Dequeue();
//				if (width > 256) { width = 256; }
//				int height = _outqueue.Dequeue();
//				if (height > 192) { height = 192; }
//				int sourcedest = _outqueue.Dequeue();
//				byte[] source_buffer;
//				byte[] source_attr_ink;
//				byte[] source_attr_paper;
//				byte[] destination_buffer;
//				byte[] destination_attr_ink;
//				byte[] destination_attr_paper;
//				if ((sourcedest & 0b10) == 0)
//				{
//					source_buffer = _renderbuffer;
//					source_attr_ink = _attr_renderbuffer_ink;
//					source_attr_paper = _attr_renderbuffer_paper;
//				}
//				else
//				{
//					source_buffer = _spritebuffer;
//					source_attr_ink = _attr_spritebuffer_ink;
//					source_attr_paper = _attr_spritebuffer_paper;
//				}
//				if ((sourcedest & 0b01) == 0)
//				{
//					destination_buffer = _renderbuffer;
//					destination_attr_ink = _attr_renderbuffer_ink;
//					destination_attr_paper = _attr_renderbuffer_paper;
//				}
//				else
//				{
//					destination_buffer = _spritebuffer;
//					destination_attr_ink = _attr_spritebuffer_ink;
//					destination_attr_paper = _attr_spritebuffer_paper;
//				}
//				int attrblock = 0;
//				if (VideoMode == 0)
//				{
//					attrblock = 8;
//				}
//				else
//				{
//					attrblock = 4;
//				}

//				sx = (sx / attrblock) * attrblock;
//				sy = (sy / attrblock) * attrblock;
//				dx = (dx / attrblock) * attrblock;
//				dy = (dy / attrblock) * attrblock;
//				width = (width / attrblock) * attrblock;
//				height = (height / attrblock) * attrblock;
//				if ((sx > 256 - width) || (sy > 192 - height) || (dx > 256 - width) || (dy > 192 - height))
//				{
//					_errorcode = ELOKEY_ERRORCODE.ARGPARM;
//					_debugdevice.DebugOut($"{srcmethod} : ARGERR");
//					return;
//				}
//				for (int y = 0; y < height; y++)
//				{
//					for (int x = 0; x < width; x++)
//					{
//						destination_buffer[(dy + y) * 256 + (dx + x)] = source_buffer[(sy + y) * 256 + (sx + x)];
//					}
//				}
//				sx = sx / attrblock;
//				sy = sy / attrblock;
//				dx = dx / attrblock;
//				dy = dy / attrblock;
//				int blockwidth = width / attrblock;
//				int blockheight = height / attrblock;
//				if (VideoMode == 0)
//				{
//					for (int y = 0; y < blockheight; y++)
//					{
//						for (int x = 0; x < blockwidth; x++)
//						{
//							destination_attr_ink[(dy + y) * _attrblockstride + (dx + x)] = source_attr_ink[(sy + y) * _attrblockstride + (sx + x)];
//						}
//					}
//				}
//				else
//				{
//					for (int y = 0; y < blockheight; y++)
//					{
//						for (int x = 0; x < blockwidth; x++)
//						{
//							destination_attr_ink[(dy + y) * _attrblockstride + (dx + x)] = source_attr_ink[(sy + y) * _attrblockstride + (sx + x)];
//							destination_attr_paper[(dy + y) * _attrblockstride + (dx + x)] = source_attr_paper[(sy + y) * _attrblockstride + (sx + x)];
//						}
//					}
//				}
//			}
//		}

//		private void LoadScreenToBuffer(byte[] bitmapbuffer, byte[] attributebuffer_ink, byte[] attributebuffer_paper)
//		{
//			string srcmethod = $"{nameof(LoadScreenToBuffer)}";
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 3)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				byte source = _outqueue.Dequeue();
//				ushort address = ZUT.ReadUIntegerFromOutQueue(_outqueue);

//				IList<byte> sourcebuffer = null;

//				switch (source)
//				{
//					case 0: //ROM 0
//						sourcebuffer = _rombanks[0];
//						break;
//					case 1: //ROM 1
//						sourcebuffer = _rombanks[1];
//						break;
//					case 2: //Disk Drive
//						sourcebuffer = _diskdrive;
//						break;
//					case 3: //ZX RAM
//						sourcebuffer = _hostram;
//						break;
//					case 4: //Slow RAM
//						sourcebuffer = _zxitram;
//						break;
//				}

//				//Convert scr to _renderbuffer
//				ZxScreenConversion.ConvertZXBitmapToLinearPixelmap(sourcebuffer, bitmapbuffer);
//				if (VideoMode == 0)
//				{
//					for (int c = 0; c < 768; c++)
//					{
//						attributebuffer_ink[c] = sourcebuffer[address + 6144 + c];
//					}
//				}
//				else
//				{
//					//ZXIT:TODO
//				}
//			}
//		}
//		public static void UnpackSrcVM1(byte[] input, out byte[] arr1, out byte[] arr2)
//		{
//			if (input.Length % 3 != 0)
//			{
//				throw new ArgumentException("Input length must be multiple of 3");
//			}	

//			int groups = input.Length / 3;

//			arr1 = new byte[groups * 2];
//			arr2 = new byte[groups * 2];

//			int o1 = 0;
//			int o2 = 0;

//			for (int i = 0; i < input.Length; i += 3)
//			{
//				byte b0 = input[i];
//				byte b1 = input[i + 1];
//				byte b2 = input[i + 2];

//				byte c1 = (byte)((((b2 >> 6) & 0x03) << 4) | (b0 & 0x0F));
//				byte c2 = (byte)((((b2 >> 4) & 0x03) << 4) | ((b0 >> 4) & 0x0F));
//				byte c3 = (byte)((((b2 >> 2) & 0x03) << 4) | (b1 & 0x0F));
//				byte c4 = (byte)(((b2 & 0x03) << 4) | ((b1 >> 4) & 0x0F));

//				arr1[o1++] = c1;
//				arr1[o1++] = c3;

//				arr2[o2++] = c2;
//				arr2[o2++] = c4;
//			}
//		}
//		public static void PackScrVM1(byte[] input, byte[] arr1, byte[] arr2)
//		{
//			if (input.Length % 3 != 0)
//			{
//				throw new ArgumentException("Input length must be a multiple of 3");
//			}
				
//			int groups = input.Length / 3;

//			if (arr1.Length < groups * 2 || arr2.Length < groups * 2)
//			{
//				throw new ArgumentException("Output arrays are too small");
//			}

//			int o1 = 0;
//			int o2 = 0;

//			for (int i = 0; i < input.Length; i += 3)
//			{
//				byte b0 = input[i];
//				byte b1 = input[i + 1];
//				byte b2 = input[i + 2];

//				byte c1 = (byte)((((b2 >> 6) & 0x03) << 4) | (b0 & 0x0F));
//				byte c2 = (byte)((((b2 >> 4) & 0x03) << 4) | ((b0 >> 4) & 0x0F));
//				byte c3 = (byte)((((b2 >> 2) & 0x03) << 4) | (b1 & 0x0F));
//				byte c4 = (byte)(((b2 & 0x03) << 4) | ((b1 >> 4) & 0x0F));

//				arr1[o1++] = c1;
//				arr1[o1++] = c3;

//				arr2[o2++] = c2;
//				arr2[o2++] = c4;
//			}
//		}

//		private void DebugMode()
//		{
//			string srcmethod = $"{nameof(DebugMode)}";
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 1)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				byte type = _outqueue.Dequeue();
//				if (type == 0)
//				{
//					_debugmode = false;
//					_debugdevice.DeviceHide();
//				}
//				else
//				{
//					_debugmode = true;
//					_debugdevice.DeviceShow();
//				}
//			}
//		}

//		private void DebugOut()
//		{
//			string srcmethod = $"{nameof(DebugOut)}";
//			lock (_lock)
//			{
//				_errorcode = ELOKEY_ERRORCODE.NONE;
//				if (_outqueue.Count < 3)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				byte type = _outqueue.Dequeue();
//				ushort address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				if (!_debugmode) { return; }
//				switch (type)
//				{
//					case 0: //Byte
//						_debugdevice.DebugOut($"BY : {_hostram[address]}" + System.Environment.NewLine);
//						break;
//					case 1: //UInteger
//						_debugdevice.DebugOut($"UI : {ZUT.ReadUIntegerFromMemory(_hostram,address)}" + System.Environment.NewLine);
//						break;
//					case 2: //ULong
//						_debugdevice.DebugOut($"UL : {ZUT.ReadULongFromMemory(_hostram,address)}" + System.Environment.NewLine);
//						break;
//					case 3: //Float
//						_debugdevice.DebugOut($"FL : {ZUT.ReadZXFloatFromMemory(_hostram,address)}" + System.Environment.NewLine);
//						break;
//					case 4: //String
//						_debugdevice.DebugOut($"ST : {ZUT.ReadStringFromMemory(_hostram,address)}" + System.Environment.NewLine);
//						break;
//				}
//			}
//		}
//		private void RndBytes()
//		{
//			string srcmethod = $"{nameof(RndBytes)}";
//			lock (_lock)
//			{
//				if (_outqueue.Count < 1)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				byte bytecount = _outqueue.Dequeue();
//				if (bytecount == 0) { return; }
//				_rndbytes = new byte[bytecount];
//				_rng.NextBytes(_rndbytes);
//				for (int c = 0; c < _rndbytes.Length; c++)
//				{
//					_inqueue.Enqueue(_rndbytes[c]);
//				}
//			}
//		}
//		private void RndBytesLimited()
//		{
//			string srcmethod = $"{nameof(RndBytesLimited)}";
//			lock (_lock)
//			{
//				if (_outqueue.Count < 2)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				byte bytecount = _outqueue.Dequeue();
//				byte bytelimit = _outqueue.Dequeue();
//				if (bytecount == 0) { return; }
//				for (int c = 0; c < bytecount; c++)
//				{
//					_inqueue.Enqueue((byte)(_rng.Next(0,bytelimit+1)));
//				}
//			}
//		}
//		private void TimeTicks()
//		{
//			lock (_lock)
//			{
//				UInt32 elap = (UInt32)(_ulazxstopwatch.ElapsedTicks);
//				ZUT.WriteULongToInQueue(_inqueue,elap);
//			}
//		}

//		//Anything bellow is the Wild Wild West. If there's nothing bellow, we're done cleaning up...

//		private void CopySpriteToRender()
//		{
//			lock (_lock)
//			{
//				for (int y = 0; y < 192; y++)
//				{
//					for (int x = 0; x < 256; x++)
//					{
//						_renderbuffer[y * 256 + x] = _spritebuffer[y * 256 + x];
//					}
//				}
//			}
//		}
//		private bool LoadRom(int bank, string file)
//		{
//			if (File.Exists(file))
//			{
//				long length = 0;
//				byte[] filebytes = null;
//				using (FileStream fs = File.OpenRead(file))
//				{
//					if (fs != null)
//					{
//						length = fs.Length;
//						if (length > 0 && length<=64*1024) //64KB Limit on ROM
//						{
//							filebytes= new byte[length];
//							fs.Read(filebytes, 0, (int)length);
//						}
//					}
//				}
//				if (length != 0)
//				{
//					_rombanks[bank] = filebytes;
//					return true;
//				}
//				else
//				{
//					return false;
//				}
//			}
//			return false;
//		}
//		private bool LoadDisk(string file)
//		{
//			return _diskdrive.Load(file);
//		}

//		private void EvaluateExpression()
//		{
//			lock (_lock)
//			{
//				string expressionstring = string.Empty;
//				string resultstring = string.Empty;

//				while (_outqueue.Count > 0)
//				{
//					byte tempbyte = _outqueue.Dequeue();
//					if (tempbyte == 0) { break; }
//					expressionstring += (char)(tempbyte);
//				}
//				ushort outaddress=ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				var K = outaddress;
//				//Safe replacement
//				expressionstring = expressionstring.ToLower();
//				expressionstring = expressionstring.Replace("sin(", "Sin(");
//				expressionstring = expressionstring.Replace("cos(", "Cos(");
//				expressionstring = expressionstring.Replace("tan(", "Tan(");
//				expressionstring = expressionstring.Replace("asin(", "Asin(");
//				expressionstring = expressionstring.Replace("acor(", "Acos(");
//				expressionstring = expressionstring.Replace("atan(", "Atan(");
//				expressionstring = expressionstring.Replace("sqr(", "Sqrt(");
//				expressionstring = expressionstring.Replace("sqrt(", "Sqrt(");

//				try
//				{
//					var expression = new Expression(expressionstring);
//					double result2 = (double)(expression.Evaluate());
//					resultstring=result2.ToString();
//				}
//				catch(Exception ex)
//				{
//					resultstring = "NAN";
//				}

//				//Clear buffer
//				for (int c = 0; c < 64; c++)
//				{
//					_hostram[outaddress + c]=0;
//				}
//				ZUT.WriteStringToMemory(_hostram,outaddress, resultstring);
//			}
//		}

//		private void RndByte()
//		{
//			_rndbytes = new byte[1];
//			_rng.NextBytes(_rndbytes);
//			_inqueue.Enqueue(_rndbytes[0]);
//		}
//		private void RndByteY()
//		{
//			_rndbytes = new byte[1];
//			do
//			{
//				_rng.NextBytes(_rndbytes);
//			} while (_rndbytes[0] > 191);
//			_inqueue.Enqueue(_rndbytes[0]);
//		}
		
//		private void RndByteLimited()
//		{
//			if (_outqueue.Count < 1) { _errorcode = ELOKEY_ERRORCODE.ARGNUM; return; }
//			byte limit = _outqueue.Dequeue();
//			_rndbytes = new byte[1];
//			do
//			{
//				_rng.NextBytes(_rndbytes);
//			} while (_rndbytes[0] > limit );
//			_inqueue.Enqueue(_rndbytes[0]);
//		}

//		private void PatternFill(int patternindex, byte[] buffer)
//		{
//			int py = (patternindex & ULAZX_FILLPATTERNINDEXY) >> 3;
//			int px = patternindex & ULAZX_FILLPATTERNINDEXX;
//			for (int y = 0; y < 192; y++)
//			{
//				for (int x = 0; x < 256; x++)
//				{
//					if (buffer[y * 256 + x] == 1)
//					{
//						if (_spritebuffer[(py * 8 + y % 8) * 256 + (px * 8 + x % 8)] == 1)
//						{
//							buffer[y * 256 + x] = 1;
//						}
//						else
//						{
//							buffer[y * 256 + x] = 2;
//						}
//					}
//				}
//			}
//		}
//		private void ClearPolyBuffer()
//		{
//			for (int y = 0; y < 192; y++)
//			{
//				for (int x = 0; x < 256; x++)
//				{
//					_fillbuffer[y * 256 + x] = 0;
//				}
//			}
//		}
		
//		private void CopyPolyToBack()
//		{
//			for (int y = 0; y < 192; y++)
//			{
//				for (int x = 0; x < 256; x++)
//				{
//					if (_fillbuffer[y * 256 + x] == 1)
//					{
//						_renderbuffer[y * 256 + x] = 1;
//					}
//					else
//					{
//						if (_fillbuffer[y * 256 + x] == 2)
//						{
//							_renderbuffer[y * 256 + x] = 0;
//						}
//					}
//				}
//			}
//		}
//		public void FloodFill()
//		{

//		}
//		public void ScanlineFloodFill(byte x, byte y, byte[] buffer)
//		{
//			if (buffer == null) { throw new ArgumentNullException(nameof(buffer)); }

//			int startX = x;
//			int startY = y;

//			if ((uint)startX >= 256u || (uint)startY >= 192u) { return; }
//			if (buffer[startY * 256 + startX] != 0) { return; }

//			Stack<int> stack = new Stack<int>();

//			stack.Push(startX);
//			stack.Push(startY);

//			while (stack.Count > 0)
//			{
//				int cy = stack.Pop();
//				int cx = stack.Pop();

//				int left = cx;
//				int right = cx;

//				while (left > 0 && buffer[cy * 256 + left - 1] == 0) { left--; }
//				while (right < 255 && buffer[cy * 256 + right + 1] == 0) { right++; }

//				for (int ix = left; ix <= right; ix++)
//				{
//					buffer[cy * 256 + ix] = 1;
//					if (VideoMode == 0)
//					{
//						_attr_renderbuffer_ink[((cy) / _attrblockh) * _attrblockstride + (ix / _attrblockw)] = _hostram[ATTR_P_VM0];
//					}
//					else
//					{
//						SetAttribute((byte)ix, (byte)cy);
//					}
//				}

//				if (cy > 0)
//				{
//					ScanAndPush(left, right, cy - 1, buffer, stack);
//				}

//				if (cy < 191)
//				{
//					ScanAndPush(left, right, cy + 1, buffer, stack);
//				}
//			}
//		}
//		private void ScanAndPush(int left, int right, int y, byte[] buffer, Stack<int> stack)
//		{
//			int x = left;

//			while (x <= right)
//			{
//				while (x <= right && buffer[y * 256 + x] != 0) { x++; }

//				if (x > right) { return; }

//				int startX = x;

//				while (x <= right && buffer[y * 256 + x] == 0) { x++; }

//				stack.Push(startX);
//				stack.Push(y);
//			}
//		}
//		public void GetCentroid(IList<PointByte2D> vertices, out double centerX, out double centerY)
//		{
//			if (vertices == null) { throw new ArgumentNullException(nameof(vertices)); }
//			if (vertices.Count < 3) { throw new ArgumentException("A polygon must have at least 3 vertices."); }

//			double areaAccumulator = 0.0;
//			double cxAccumulator = 0.0;
//			double cyAccumulator = 0.0;

//			int count = vertices.Count;

//			for (int i = 0; i < count; i++)
//			{
//				PointByte2D current = vertices[i];
//				PointByte2D next = vertices[(i + 1) % count];

//				double xi = current.X;
//				double yi = current.Y;
//				double xj = next.X;
//				double yj = next.Y;

//				double cross = (xi * yj) - (xj * yi);

//				areaAccumulator += cross;
//				cxAccumulator += (xi + xj) * cross;
//				cyAccumulator += (yi + yj) * cross;
//			}

//			double area = areaAccumulator * 0.5;

//			if (Math.Abs(area) < double.Epsilon)
//			{
//				double sumX = 0.0;
//				double sumY = 0.0;

//				for (int i = 0; i < count; i++)
//				{
//					sumX += vertices[i].X;
//					sumY += vertices[i].Y;
//				}

//				centerX = sumX / count;
//				centerY = sumY / count;
//				return;
//			}

//			double factor = 1.0 / (6.0 * area);

//			centerX = cxAccumulator * factor;
//			centerY = cyAccumulator * factor;
//		}
//		//#region FPU
//		//public void FloatAdd()
//		//{
//		//	string srcmethod = $"{nameof(FloatAdd)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address =ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		var float1=ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		var res = float1 + float2;
//		//		WriteZXFloatToMemory(float3address,res);
//		//	}
//		//}
//		//public void FixedAdd()
//		//{
//		//	string srcmethod = $"{nameof(FixedAdd)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		ushort fixed3address = ReadUIntegerFromOutQueue();
//		//		double float1=ReadZXFixedFromMemory(fixed1address);
//		//		double float2 = ReadZXFloatFromMemory(fixed2address);
//		//		var res = float1 + float2;
//		//		WriteZXFixedToMemory(fixed3address, res);
//		//	}
//		//}
//		//public void FloatSub()
//		//{
//		//	string srcmethod = $"{nameof(FloatSub)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		var res = float1 - float2;
//		//		WriteZXFloatToMemory(float3address, res);
//		//	}
//		//}
//		//public void FixedSub()
//		//{
//		//	string srcmethod = $"{nameof(FixedSub)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		ushort fixed3address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double float2 = ReadZXFloatFromMemory(fixed2address);
//		//		var res = float1 - float2;
//		//		WriteZXFixedToMemory(fixed3address, res);
//		//	}
//		//}
//		//public void FloatMul()
//		//{
//		//	string srcmethod = $"{nameof(FloatMul)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		var res = float1 * float2;
//		//		WriteZXFloatToMemory(float3address, res);
//		//	}
//		//}
//		//public void FixedMul()
//		//{
//		//	string srcmethod = $"{nameof(FixedMul)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		ushort fixed3address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double float2 = ReadZXFloatFromMemory(fixed2address);
//		//		var res = float1 * float2;
//		//		WriteZXFixedToMemory(fixed3address, res);
//		//	}
//		//}
//		//public void FloatDiv()
//		//{
//		//	string srcmethod = $"{nameof(FloatDiv)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		double res = 0;
//		//		if (float2 != 0) { res = float1 / float2; }
//		//		WriteZXFloatToMemory(float3address, res);
//		//	}
//		//}
//		//public void FixedDiv()
//		//{
//		//	string srcmethod = $"{nameof(FixedDiv)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		ushort fixed3address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double float2 = ReadZXFloatFromMemory(fixed2address);
//		//		double res = 0;
//		//		if (float2 != 0) { res = float1 / float2; }
//		//		WriteZXFixedToMemory(fixed3address, res);
//		//	}
//		//}
//		//public void FloatPow()
//		//{
//		//	string srcmethod = $"{nameof(FloatPow)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		double res = Math.Pow(float1, float2);
//		//		WriteZXFloatToMemory(float3address, res);
//		//	}
//		//}
//		//public void FixedPow()
//		//{
//		//	string srcmethod = $"{nameof(FixedPow)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		ushort fixed3address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double float2 = ReadZXFloatFromMemory(fixed2address);
//		//		double res = Math.Pow(float1, float2);
//		//		WriteZXFixedToMemory(fixed3address, res);
//		//	}
//		//}
//		//public void FloatExp()
//		//{
//		//	string srcmethod = $"{nameof(FloatExp)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Exp(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedExp()
//		//{
//		//	string srcmethod = $"{nameof(FixedExp)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Exp(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatLn()
//		//{
//		//	string srcmethod = $"{nameof(FloatLn)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Log(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedLn()
//		//{
//		//	string srcmethod = $"{nameof(FixedLn)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Log(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatSin()
//		//{
//		//	string srcmethod = $"{nameof(FloatSin)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Sin(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedSin()
//		//{
//		//	string srcmethod = $"{nameof(FixedSin)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Sin(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatCos()
//		//{
//		//	string srcmethod = $"{nameof(FloatCos)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Cos(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedCos()
//		//{
//		//	string srcmethod = $"{nameof(FixedCos)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Cos(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatTan()
//		//{
//		//	string srcmethod = $"{nameof(FloatTan)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Tan(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedTan()
//		//{
//		//	string srcmethod = $"{nameof(FixedTan)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Tan(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatAsn()
//		//{
//		//	string srcmethod = $"{nameof(FloatAsn)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Asin(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedAsn()
//		//{
//		//	string srcmethod = $"{nameof(FixedAsn)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Asin(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatAcs()
//		//{
//		//	string srcmethod = $"{nameof(FloatAcs)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Acos(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedAcs()
//		//{
//		//	string srcmethod = $"{nameof(FixedAcs)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Acos(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatAtn()
//		//{
//		//	string srcmethod = $"{nameof(FloatAtn)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		double res = Math.Atan(float1);
//		//		WriteZXFloatToMemory(float2address, res);
//		//	}
//		//}
//		//public void FixedAtn()
//		//{
//		//	string srcmethod = $"{nameof(FixedAtn)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 4)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort fixed1address = ReadUIntegerFromOutQueue();
//		//		ushort fixed2address = ReadUIntegerFromOutQueue();
//		//		double float1 = ReadZXFixedFromMemory(fixed1address);
//		//		double res = Math.Atan(float1);
//		//		WriteZXFixedToMemory(fixed2address, res);
//		//	}
//		//}
//		//public void FloatDotProductV3()
//		//{
//		//	string srcmethod = $"{nameof(FloatDotProductV3)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 6)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort V1address = ReadUIntegerFromOutQueue();
//		//		ushort V2address = ReadUIntegerFromOutQueue();
//		//		ushort destaddress = ReadUIntegerFromOutQueue();

//		//		var V1X = ReadZXFloatFromMemory(V1address);
//		//		var V1Y = ReadZXFloatFromMemory((ushort)(V1address + 5));
//		//		var V1Z = ReadZXFloatFromMemory((ushort)(V1address + 10));

//		//		var V2X = ReadZXFloatFromMemory(V2address);
//		//		var V2Y = ReadZXFloatFromMemory((ushort)(V2address + 5));
//		//		var V2Z = ReadZXFloatFromMemory((ushort)(V2address + 10));

//		//		double res = V1X * V2X + V1Y * V2Y + V1Z * V2Z;
//		//		WriteZXFloatToMemory(destaddress, res);
//		//	}
//		//}
//		//public void FloatNormalizeV3()
//		//{
//		//	string srcmethod = $"{nameof(FloatNormalizeV3)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 2)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		double res = 0;
//		//		ushort V1address = ReadUIntegerFromOutQueue();
//		//		ushort destaddress = ReadUIntegerFromOutQueue();
//		//		var V1X = ReadZXFloatFromMemory(V1address);
//		//		var V1Y = ReadZXFloatFromMemory((ushort)(V1address + 5));
//		//		var V1Z = ReadZXFloatFromMemory((ushort)(V1address + 10));
//		//		float mag = (float)Math.Sqrt(V1X * V1X + V1Y * V1Y + V1Z * V1Z);
//		//		float invLen = (mag > 0.00001f) ? 1.0f / mag : 0.0f;
//		//		res = V1X * invLen;
//		//		WriteZXFloatToMemory(destaddress, res);
//		//		res = V1Y * invLen;
//		//		WriteZXFloatToMemory((ushort)(destaddress + 5), res);
//		//		res = V1Z * invLen;
//		//		WriteZXFloatToMemory((ushort)(destaddress + 10), res);
//		//	}
//		//}
//		//public void FloatRaySphereHit()
//		//{
//		//	string srcmethod = $"{nameof(FloatRaySphereHit)}";
//		//	lock (_lock)
//		//	{
//		//		_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//		if (_outqueue.Count < 12)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		double res = 0;
//		//		ushort origPtr = ReadUIntegerFromOutQueue();
//		//		Vector3 O = ReadVector3FromMemory(origPtr);
//		//		ushort dirPtr = ReadUIntegerFromOutQueue();
//		//		Vector3 D = ReadVector3FromMemory(dirPtr);
//		//		ushort sPosPtr = ReadUIntegerFromOutQueue();
//		//		Vector3 S = ReadVector3FromMemory(sPosPtr);
//		//		ushort sRadPtr = ReadUIntegerFromOutQueue();
//		//		float R2 = (float)ReadZXFloatFromMemory(sRadPtr);
//		//		ushort nearTPtr = ReadUIntegerFromOutQueue();
//		//		ushort farTPtr = ReadUIntegerFromOutQueue();

//		//		Vector3 L = S - O;
//		//		float tca = Vector3.Dot(L, D);

//		//		// Initial check: if tca < 0, the sphere is behind the ray origin
//		//		if (tca < 0)
//		//		{
//		//			WriteZXFloatToMemory(nearTPtr, -1.0f);
//		//			WriteZXFloatToMemory(farTPtr, -1.0f);
//		//			return;
//		//		}

//		//		float d2 = Vector3.Dot(L, L) - (tca * tca);
//		//		if (d2 > R2)
//		//		{
//		//			WriteZXFloatToMemory(nearTPtr, -1.0f);
//		//			WriteZXFloatToMemory(farTPtr, -1.0f);
//		//			return;
//		//		}

//		//		float thc = (float)Math.Sqrt(R2 - d2);

//		//		// Write results to their independent destinations
//		//		WriteZXFloatToMemory(nearTPtr, tca - thc);
//		//		WriteZXFloatToMemory(farTPtr, tca + thc);
//		//	}
//		//}
//		//public void FloatMulAdd()
//		//{
//		//	//A*B+C
//		//	string srcmethod = $"{nameof(FloatMulAdd)}";
//		//	_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//	lock (_lock)
//		//	{
//		//		if (_outqueue.Count < 8)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		ushort floatdestinationaddress = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		var float3 = ReadZXFloatFromMemory(float3address);
//		//		double res = float1 * float2 + float3;
//		//		WriteZXFloatToMemory(floatdestinationaddress, res);
//		//	}
//		//}
//		//public void FloatInRange()
//		//{
//		//	//min<val<max
//		//	string srcmethod = $"{nameof(FloatInRange)}";
//		//	_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//	lock (_lock)
//		//	{
//		//		if (_outqueue.Count < 8)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		ushort bytedestinationaddress = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		var float3 = ReadZXFloatFromMemory(float3address);
//		//		byte outbyte = 0;
//		//		if (float1 > float2 && float1 < float3) { outbyte = 1; } else { outbyte = 0; }
//		//		_hostram[bytedestinationaddress]=outbyte;
//		//	}
//		//}
//		//public void FloatSumOfProducts3()
//		//{
//		//	//RES=A*B+C*D+E*F
//		//	string srcmethod = $"{nameof(FloatSumOfProducts3)}";
//		//	_errorcode = EULAZX_ERRORCODE.ULAZXEC_NONE;
//		//	lock (_lock)
//		//	{
//		//		if (_outqueue.Count < 14)
//		//		{
//		//			ArgNumError($"{srcmethod}");
//		//			_outqueue.Clear();
//		//			return;
//		//		}
//		//		ushort float1address = ReadUIntegerFromOutQueue();
//		//		ushort float2address = ReadUIntegerFromOutQueue();
//		//		ushort float3address = ReadUIntegerFromOutQueue();
//		//		ushort float4address = ReadUIntegerFromOutQueue();
//		//		ushort float5address = ReadUIntegerFromOutQueue();
//		//		ushort float6address = ReadUIntegerFromOutQueue();
//		//		ushort floatdestinationaddress = ReadUIntegerFromOutQueue();
//		//		var float1 = ReadZXFloatFromMemory(float1address);
//		//		var float2 = ReadZXFloatFromMemory(float2address);
//		//		var float3 = ReadZXFloatFromMemory(float3address);
//		//		var float4 = ReadZXFloatFromMemory(float4address);
//		//		var float5 = ReadZXFloatFromMemory(float5address);
//		//		var float6 = ReadZXFloatFromMemory(float6address);
//		//		double res = ((float1 * float2) + (float3*float4)+(float5*float6));
//		//		WriteZXFloatToMemory(floatdestinationaddress, res);
//		//	}
//		//}
//		//#endregion
//		private void RandomAttributes()
//		{
//			for (int index = 0; index < _attr_renderbuffer_ink.Length; index++)
//			{
//				if (VideoMode == 0)
//				{
//					_attr_renderbuffer_ink[index] = (byte)_rng.Next(0, 256);
//				}
//				else
//				{
//					_attr_renderbuffer_ink[index] = (byte)_rng.Next(0, 64);
//					_attr_renderbuffer_paper[index] = (byte)_rng.Next(0, 64);
//				}
//			}
//		}

//		private void RandomBitmap()
//		{
//			for (int y = 0; y < 192; y++)
//			{
//				for (int x = 0; x < 256; x++)
//				{
//					_renderbuffer[y * 256 + x] = (byte)_rng.Next(0, 2);
//				}
//			}
//		}
//		private void RandomBuffer()
//		{
//			RandomAttributes();
//			RandomBitmap();
//		}

//		private void Print(bool user,bool masked)
//		{
//			string srcmethod = $"{nameof(Print)}";
//			_errorcode = ELOKEY_ERRORCODE.NONE;
//			lock (_lock)
//			{
//				if (_outqueue.Count < 3)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				byte charindex = _outqueue.Dequeue();
//				byte x = _outqueue.Dequeue();
//				byte y = _outqueue.Dequeue();

//				byte[] rombuffer=null;

//				//User rom font
//				if (user && charindex > 127)
//				{
//					ArgParmError(srcmethod, "charindex>127 with user rom font");
//					return;
//				}

//				if (user) { rombuffer = _userromfont; } else { rombuffer = _systemromfont; }

//				int bufferindex = charindex * 64; //Because we use bytes as bits internally, or it would be *8

//				//Unified print logic, if masked its the exact same, if not we set attributes based on _attrblockw and _attrblockh
//				//We duplicate the loop so that we have less branching as there's one loop for masked and a different one for non masked

//				if (masked)
//				{
//					//Masked print does NOT set attributes
//					for (int yc = 0; yc < 8; yc++)
//					{
//						for (int xc = 0; xc < 8; xc++)
//						{
//							if (rombuffer[bufferindex] == 1)
//							{
//								//Inside screen?
//								if ((y + yc) < 192 && (x + xc) < 256)
//								{
//									_renderbuffer[(y + yc) * 256 + (x + xc)] = 1;
//								}
//							}
//							bufferindex++;
//						}
//					}
//				}
//				else
//				{
//					for (int yc = 0; yc < 8; yc++)
//					{
//						for (int xc = 0; xc < 8; xc++)
//						{
//							if ((y + yc) < 192 && (x + xc) < 256)
//							{
//								_renderbuffer[(y + yc) * 256 + (x + xc)] = _systemromfont[bufferindex];
//								if (VideoMode == 0)
//								{
//									_attr_renderbuffer_ink[((y + yc) / _attrblockh) * _attrblockstride + ((x + xc) / _attrblockw)] = _hostram[ATTR_P_VM0];
//								}
//								else
//								{
//									_attr_renderbuffer_ink[((y + yc) / _attrblockh) * _attrblockstride + ((x + xc) / _attrblockw)] =_hostram[_attr_p_vm1_I];
//									_attr_renderbuffer_paper[((y + yc) / _attrblockh) * _attrblockstride + ((x + xc) / _attrblockw)] = _hostram[_attr_p_vm1_P];
//								}
//							}
//							bufferindex++;
//						}
//					}
//				}

					
//			}
//		}
//		private void PrintScreen(bool user)
//		{
//			string srcmethod = $"{nameof(PrintScreen)}";
//			_errorcode = ELOKEY_ERRORCODE.NONE;
//			lock (_lock)
//			{
//				if (_outqueue.Count < 4)
//				{
//					ArgNumError($"{srcmethod}");
//					_outqueue.Clear();
//					return;
//				}
//				ushort charbufferaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
//				ushort attrbufferaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);

//				byte[] rombuffer = null;

//				if (user) { rombuffer = _userromfont; } else { rombuffer = _systemromfont; }

//				int index = 0;

//				for(int row = 0; row < 24; row++)
//				{
//					for (int col = 0; col < 32; col++)
//					{
//						int fontbufferindex = _hostram[(ushort)(charbufferaddress + index)] * 64;

//						if (VideoMode == 0)
//						{
//							byte attribute =  _hostram[(ushort)(attrbufferaddress + index)];
//							_attr_renderbuffer_ink[row * 32 + col] = attribute;
//						}
//						else
//						{
//						}
//						for (int yc = 0; yc < 8; yc++)
//						{
//							for (int xc = 0; xc < 8; xc++)
//							{
//								if ((row * 8 + yc) < 192 && (col * 8 + xc) < 256)
//								{
//									_renderbuffer[(row * 8 + yc) * 256 + (col * 8 + xc)] = rombuffer[fontbufferindex];
//									fontbufferindex++;
//								}
//							}
//						}
//						index++;
//					}
//				}
//			}
//		}

//		public void LoadFontSystemRom(byte[] romfont, string romfontfile)
//		{
//			if (romfont == null || romfont.Length < 2048 * 8) { throw new Exception("Rom font buffer was null or <2048"); }

//			int index = 0;
//			byte[] romfontbytes = File.ReadAllBytes(romfontfile);
//			for (int c = 0; c < romfontbytes.Length; c++)
//			{
//				string asstring = Convert.ToString(romfontbytes[c], 2).PadLeft(8, '0');
//				for (int bitval = 0; bitval < 8; bitval++)
//				{
//					if (asstring[bitval] == '0')
//					{
//						romfont[index] = 0; index++;
//					}
//					else
//					{
//						romfont[index] = 1; index++;
//					}
//				}
//			}
//		}
//		public void LoadFontUserRom(byte[] romfont, string romfontfile)
//		{
//			if (romfont == null || romfont.Length < (2048 * 8)) { throw new Exception("Rom font buffer was null or <2048"); }

//			int index = 32 * 64;
//			byte[] romfontbytes = File.ReadAllBytes(romfontfile);
//			for (int c = 0; c < romfontbytes.Length; c++)
//			{
//				string asstring = Convert.ToString(romfontbytes[c], 2).PadLeft(8, '0');
//				for (int bitval = 0; bitval < 8; bitval++)
//				{
//					if (asstring[bitval] == '0')
//					{
//						romfont[index] = 0; index++;
//					}
//					else
//					{
//						romfont[index] = 1; index++;
//					}
//				}
//			}
//			index = 0;
//			for (int c = 0; c < userrombasedata.Length; c++)
//			{
//				string asstring = Convert.ToString(userrombasedata[c], 2).PadLeft(8, '0');
//				for (int bitval = 0; bitval < 8; bitval++)
//				{
//					if (asstring[bitval] == '0')
//					{
//						romfont[index] = 0; index++;
//					}
//					else
//					{
//						romfont[index] = 1; index++;
//					}
//				}
//			}

//		}
//		public void Cls()
//		{
//			lock (_lock)
//			{
//				ClearBitmap(false);
//				ClearAttributes(false);
//			}
//		}
//		public void ClearBitmap(bool reset)
//		{
//			for (int y = 0; y < 192; y++)
//			{
//				for (int x = 0; x < 256; x++)
//				{
//					_renderbuffer[y * 256 + x] = 0;
//				}
//			}
//			if (reset)
//			{
//				for (int y = 0; y < 192; y++)
//				{
//					for (int x = 0; x < 256; x++)
//					{
//						_spritebuffer[y * 256 + x] = 0;
//					}
//				}
//			}
//			ResetPatterns();
//		}
//		public void ClearAttributes(bool reset)
//		{
//			if (!reset)
//			{
//				if (VideoMode == 0)
//				{
//					for (int index = 0; index < _attr_renderbuffer_ink.Length; index++)
//					{
//						_attr_renderbuffer_ink[index] = _hostram[ATTR_P_VM0];
//					}
//				}
//				else
//				{
//					for (int index = 0; index < _attr_renderbuffer_ink.Length; index++)
//					{
//						_attr_renderbuffer_ink[index] = _hostram[_attr_p_vm1_I];
//						_attr_renderbuffer_paper[index] = _hostram[_attr_p_vm1_P];
//					}
//				}
//			}
//			else
//			{
//				int paper = 7;
//				byte tempattr= (byte)(paper<<3);
//				if (VideoMode == 0)
//				{
//					for (int index = 0; index < _attr_renderbuffer_ink.Length; index++)
//					{
//						_attr_renderbuffer_ink[index] = tempattr;
//						_attr_spritebuffer_ink[index] = tempattr;
//					}
//				}
//				else
//				{
//					for (int index = 0; index < _attr_renderbuffer_ink.Length; index++)
//					{
//						_attr_renderbuffer_ink[index] = 0;
//						_attr_renderbuffer_paper[index] = 7;
//						_attr_spritebuffer_ink[index] = 0;
//						_attr_spritebuffer_paper[index] = 7;
//					}
//				}
//			}
//		}
//		#region Bias
//		private void BiasSetPositive()
//		{
//			_bias = _outqueue.Dequeue();
//		}
//		private void BiasSetNegative()
//		{
//			_bias = -1 * _outqueue.Dequeue();
//		}
//		#endregion
//		public int GetPalette(int paletteentryindex)
//		{
//			tr = _palette0[paletteentryindex, 0];
//			tr = tr + _bias;
//			if (tr < 0) { tr = 0; }
//			else { if (tr > 255) { tr = 255; } }
//			tg = _palette0[paletteentryindex, 1];
//			tg = tg + _bias;
//			if (tg < 0) { tg = 0; }
//			else { if (tg > 255) { tg = 255; } }
//			tb = _palette0[paletteentryindex, 2];
//			tb = tb + _bias;
//			if (tb < 0) { tb = 0; }
//			else { if (tb > 255) { tb = 255; } }
//			int tempcolor = tr * 65536 + tg * 256 + tb;
//			return tempcolor;
//		}
//		public void Reset()
//		{
//			VideoMode = 0;
//			_bias = 0;
//			ClearBitmap(true);
//			ClearAttributes(true);
//			_attr_p_vm1_I = 23728;
//			_attr_p_vm1_P = 23729;
//		}
//		public void RegisterDevice(zx_spectrum hostmachine)
//		{
//			hostmachine.io_devices.Remove(this);
//			hostmachine.io_devices.Add(this);
//			_host = hostmachine;
//			_hostram=new ZxRam(hostmachine);
//		}
//		public void UnregisterDevice(zx_spectrum hostmachine)
//		{
//			hostmachine.io_devices.Remove(this);
//			_host = null;
//			_hostram=null;
//		}
//		private byte[] IntToBytes(int value)
//		{
//			byte[] bytes = BitConverter.GetBytes(value);
//			return new byte[] { bytes[2], bytes[1], bytes[0] };
//		}
//		private int BytesToInt(byte[] bytes)
//		{
//			return bytes[0] * 256 * 256 + bytes[1] * 256 + bytes[2];
//		}

//		public void Shutdown()
//		{
//			_debugdevice.DeviceHide();
//		}
//		private byte[] userrombasedata = new byte[32 * 8]
//		{
//			255,128,128,128,128,128,128,128,
//			255,128,191,160,160,160,160,160,
//			255,128,191,160,175,168,168,168,
//			255,128,191,160,175,168,171,170,
//			0,127,64,64,64,64,64,64,
//			0,127,64,95,80,80,80,80,
//			0,127,64,95,80,87,84,84,
//			0,127,64,95,80,87,84,85,
//			64,128,0,0,0,0,0,0,
//			72,144,32,64,128,0,0,0,
//			73,146,36,72,144,32,64,128,
//			73,146,36,73,146,36,72,144,
//			73,146,36,73,146,36,73,146,
//			201,210,36,89,154,36,75,147,
//			217,218,36,91,155,36,75,147,
//			217,218,36,219,219,36,91,155,
//			219,219,36,219,219,36,219,219,
//			219,129,0,129,129,0,129,219,
//			219,129,60,165,165,60,129,219,
//			255,165,255,165,165,255,165,255,
//			240,0,0,0,0,0,0,15,
//			240,0,120,0,0,30,0,15,
//			241,1,255,129,129,255,128,143,
//			253,37,37,37,164,164,164,191,
//			136,34,136,34,136,34,136,34,
//			85,0,170,0,85,0,170,0,
//			136,170,170,170,170,170,170,34,
//			127,0,254,0,127,0,254,0,
//			129,66,36,24,24,24,24,255,
//			129,130,132,248,248,132,130,129,
//			255,24,24,24,24,36,66,129,
//			129,65,33,31,31,33,65,129
//		};
//		//private byte[] ReadBytesFromOutQueue(int numbytes)
//		//{
//		//	byte[] bytes = new byte[numbytes];
//		//	for (int c = 0; c < numbytes; c++)
//		//	{
//		//		bytes[c] = _outqueue.Dequeue();
//		//	}
//		//	return bytes;
//		//}
//		//private void WriteULongToInQueue(UInt32 value)
//		//{
//		//	byte[] bytes = BitConverter.GetBytes(value);
//		//	WriteBytesToInQueue(bytes);
//		//}
//		//private ushort ReadUIntegerFromOutQueue()
//		//{
//		//	byte[] bytes = ReadBytesFromOutQueue(2);
//		//	return (ushort)(bytes[0] + bytes[1] * 256);
//		//}
//		//private void WriteUIntegerToInQueue(UInt16 value)
//		//{
//		//	byte[] bytes = BitConverter.GetBytes(value);
//		//	WriteBytesToInQueue(bytes);
//		//}
//		//private void WriteBytesToInQueue(byte[] bytevalues)
//		//{
//		//	for (int c = 0; c < bytevalues.Length; c++)
//		//	{
//		//		_inqueue.Enqueue(bytevalues[c]);
//		//	}
//		//}
//		//private double ReadZXFloatFromOutQueue()
//		//{
//		//	double retval = 0.0;
//		//	byte[] bytes = this.ReadBytesFromOutQueue(5);
//		//	retval = ZXUtility.ZXFloatToDouble(bytes);
//		//	return retval;
//		//}
//		//private ushort ReadUIntegerFromMemory(ushort addr)
//		//{
//		//	ushort retval = 0;
//		//	byte[] bytes = new byte[2];
//		//	for (int c = 0; c < 2; c++)
//		//	{
//		//		bytes[c] = _hostram[(ushort)(addr + c)];
//		//	}
//		//	retval = BitConverter.ToUInt16(bytes,0);
//		//	return retval;
//		//}
//		//private ulong ReadULongFromMemory(ushort addr)
//		//{
//		//	ulong retval = 0;
//		//	byte[] bytes = new byte[4];
//		//	for (int c = 0; c < 4; c++)
//		//	{
//		//		bytes[c] = _hostram[(ushort)(addr + c)];
//		//	}
//		//	retval = BitConverter.ToUInt32(bytes, 0);
//		//	return retval;
//		//}
//		//private string ReadStringFromMemory(ushort addr)
//		//{
//		//	string retval = string.Empty;
//		//	List<byte> tempbytes= new List<byte>();
//		//	while (_hostram[(addr)] != 0)
//		//	{
//		//		tempbytes.Add(_hostram[(addr)]);
//		//		addr++;
//		//	}
//		//	retval=Encoding.ASCII.GetString(tempbytes.ToArray());
//		//	return retval;
//		//}
//		//private double ReadZXFloatFromMemory(ushort addr)
//		//{
//		//	double retval = 0.0;
//		//	byte[] bytes = new byte[5];
//		//	for (int c = 0; c < 5; c++)
//		//	{
//		//		bytes[c] = _hostram[(ushort)(addr + c)];
//		//	}
//		//	retval = ZXUtility.ZXFloatToDouble(bytes);
//		//	return retval;
//		//}
//		//private double ReadZXFixedFromMemory(ushort addr)
//		//{
//		//	double retval = 0.0;
//		//	byte[] bytes = new byte[4];
//		//	for (int c = 0; c < 4; c++)
//		//	{
//		//		bytes[c] = _hostram[(ushort)(addr + c)];
//		//	}
//		//	retval=FixedToDouble(bytes);
//		//	return retval;
//		//}
//		//private Vector3 ReadVector3FromMemory(ushort addr)
//		//{
//		//	double x = ReadZXFloatFromMemory(addr);
//		//	double y = ReadZXFloatFromMemory((ushort)(addr+5));
//		//	double z = ReadZXFloatFromMemory((ushort)(addr+10));
//		//	return new Vector3((float)x, (float)y, (float)z);
//		//}
//		//private void WriteZXFloatToInQueue(double value)
//		//{
//		//	byte[] bytes = ZXUtility.DoubleToZXFloat(Math.Round(value, 8));
//		//	WriteBytesToInQueue(bytes);
//		//}
//		//private void WriteZXFloatToMemory(ushort addr, double val)
//		//{
//		//	byte[] bytes = ZXUtility.DoubleToZXFloat(Math.Round(val, 8));
//		//	for (int c = 0; c < 5; c++)
//		//	{
//		//		_hostram[addr + c]=bytes[c];
//		//	}
//		//}
//		//private void WriteZXFixedToMemory(ushort addr, double val)
//		//{
//		//	byte[] bytes = DoubleToFixed(val);
//		//	for (int c = 0; c < 4; c++)
//		//	{
//		//		_hostram[addr + c] = bytes[c];
//		//	}
//		//}
//		//private void WriteStringToMemory(ushort addr, string outstr)
//		//{
//		//	for (int c = 0; c < outstr.Length; c++)
//		//	{
//		//		_hostram[addr + c]=(byte)outstr[c];
//		//	}
//		//}

//		public void SetVideoModeFromUI(byte mode)
//		{
//			VideoMode= mode;
//		}

//		public byte GetInkIndex(byte x, byte y)
//		{
//			return _attr_shadowbuffer_ink[(y/_attrblockh)*_attrblockstride+(x/_attrblockw)];
//		}

//		public byte GetPaperIndex(byte x, byte y)
//		{
//			return _attr_shadowbuffer_paper[(y / _attrblockh) * _attrblockstride + (x / _attrblockw)];
//		}

//		public int GetColor(byte index)
//		{
//			tr = _palette0[index, 0];
//			tr = tr + _bias;
//			if (tr < 0) { tr = 0; }
//			else { if (tr > 255) { tr = 255; } }
//			tg = _palette0[index, 1];
//			tg = tg + _bias;
//			if (tg < 0) { tg = 0; }
//			else { if (tg > 255) { tg = 255; } }
//			tb = _palette0[index, 2];
//			tb = tb + _bias;
//			if (tb < 0) { tb = 0; }
//			else { if (tb > 255) { tb = 255; } }
//			int tempcolor = tr * 65536 + tg * 256 + tb;
//			return tempcolor;
//		}
//		private void ResetPatterns()
//		{
//			PatternCheckers();
//			PatternHorizontalStripes();
//			PatternVerticalStripes();
//		}
//		private void PatternCheckers()
//		{
//			for (int y = 0; y < 8; y += 2)
//			{
//				for (int x = 0; x < 8; x += 2)
//				{
//					_spritebuffer[y * 256 + x] = 1;
//				}
//			}
//			for (int y = 1; y < 9; y += 2)
//			{
//				for (int x = 1; x < 9; x += 2)
//				{
//					_spritebuffer[y * 256 + x] = 1;
//				}
//			}
//		}
//		private void PatternHorizontalStripes()
//		{
//			for (int y = 0; y < 8; y += 2)
//			{
//				for (int x = 0; x < 8; x++)
//				{
//					_spritebuffer[y * 256 + x + 8] = 1;
//				}
//			}
//		}
//		private void PatternVerticalStripes()
//		{
//			for (int y = 0; y < 8; y++)
//			{
//				for (int x = 0; x < 8; x += 2)
//				{
//					_spritebuffer[y * 256 + x + 16] = 1;
//				}
//			}
//		}
//		//public static double FixedToDouble(byte[] fixedPointBytes)
//		//{
//		//	if (fixedPointBytes == null || fixedPointBytes.Length != 4)
//		//	{
//		//		throw new ArgumentException("Fixed point value must be represented by exactly 4 bytes.", nameof(fixedPointBytes));
//		//	}

//		//	// Boriel Basic Fixed point is 32-bit: 1 bit sign, 15 bits integer, 16 bits fractional.
//		//	// The value is stored in little-endian format.

//		//	// Reconstruct the 32-bit signed integer value
//		//	int signedValue = fixedPointBytes[0] | (fixedPointBytes[1] << 8) | (fixedPointBytes[2] << 16) | (fixedPointBytes[3] << 24);

//		//	// The fractional part has 16 bits, so the divisor is 2^16
//		//	return (double)signedValue / 65536.0;
//		//}
//		//public static byte[] DoubleToFixed(double value)
//		//{
//		//	// Boriel Basic Fixed point is 32-bit: 1 bit sign, 15 bits integer, 16 bits fractional.
//		//	// The maximum positive value for Boriel Basic Fixed point is approximately 32767.99998.
//		//	// The minimum negative value for Boriel Basic Fixed point is approximately -32768.0.

//		//	if (value > 32767.99998474121 || value < -32768.0)
//		//	{
//		//		throw new ArgumentOutOfRangeException(nameof(value), "Value is outside the representable range for Boriel Basic Fixed point (-32768.0 to 32767.99998...).");
//		//	}

//		//	// Multiply by 2^16 to shift the fractional part into the integer part.
//		//	// Then cast to int to truncate.
//		//	int signedValue = (int)Math.Round(value * 65536.0);

//		//	// Convert the 32-bit signed integer to a 4-byte little-endian array.
//		//	byte[] fixedPointBytes = new byte[4];
//		//	fixedPointBytes[0] = (byte)signedValue;
//		//	fixedPointBytes[1] = (byte)(signedValue >> 8);
//		//	fixedPointBytes[2] = (byte)(signedValue >> 16);
//		//	fixedPointBytes[3] = (byte)(signedValue >> 24);

//		//	return fixedPointBytes;
//		//}
//	}
//}

