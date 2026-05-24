using NCalc;
using Speccy;
using SpeccyCommon;
using System;
using System.Collections.Generic;
using System.Numerics;
using ZUT = LOKEY.LOKEY_Utility_Types;

namespace LOKEY
{
	public partial class LokeyFpu : IODevice
	{
		private readonly object _lock = new object();

		private bool _debugmode = false;

		private zx_spectrum _host = null;
		private ZxRam _hostram = null;

		private readonly Queue<byte> _outqueue = new Queue<byte>();
		private readonly Queue<byte> _inqueue = new Queue<byte>();
		public bool Responded { get; set; }

		private byte[] _rndbytes = new byte[16];
		private static Random _rng = new Random(321);

		Dictionary<string, Expression> _expressioncache = new Dictionary<string, Expression>();

		private ELOKEY_ERRORCODE _errorcode = ELOKEY_ERRORCODE.NONE;

		public SPECTRUM_DEVICE DeviceID { get { return SPECTRUM_DEVICE.LOKEY_FPU; } }

		private const int _dataport = 133;
		private const int _commandport = 135;

		public LokeyFpu()
		{
		}

		public void Out(ushort port, byte val)
		{
			Responded = false;
			if (port == _dataport)
			{
				_outqueue.Enqueue(val);
				Responded = true;
			}
			else if (port == _commandport)
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
				#region RNG
				case 20: RndBytes(); break;
				case 21: RndFloat(); break;
				case 22: RndFixed(); break;
				#endregion

				#region FloatOps
				case 50: FloatAdd(); break;
				case 51: FloatSub(); break;
				case 52: FloatMul(); break;
				case 53: FloatDiv(); break;
				case 54: FloatPow(); break;
				case 55: FloatExp(); break;
				case 56: FloatLn(); break;
				case 57: FloatSin(); break;
				case 58: FloatCos(); break;
				case 59: FloatTan(); break;
				case 60: FloatAsn(); break;
				case 61: FloatAcs(); break;
				case 62: FloatAtn(); break;
				case 63: FloatSqrt(); break;
				case 64: FloatAbs(); break;

				case 65: FloatCompare(); break;

				case 66: FloatMulAdd(); break; //FMA
				case 67: FloatMulSub(); break; //FMS
				case 68: FloatDivAdd(); break; //FDA
				case 69: FloatDivSub(); break; //FDS

				case 70: FloatInRange(); break;
				case 71: FloatNormalizeV3(); break;
				case 72: FloatRaySphereHit(); break;
				case 73: FloatRotate2D(); break;

				case 74: FloatSumOfProducts3(); break;

				case 100: FixedAdd(); break;
				case 101: FixedSub(); break;
				case 102: FixedMul(); break;
				case 103: FixedDiv(); break;
				case 104: FixedPow(); break;
				case 105: FixedExp(); break;
				case 106: FixedLn(); break;
				case 107: FixedSin(); break;
				case 108: FixedCos(); break;
				case 109: FixedTan(); break;
				case 110: FixedAsn(); break;
				case 111: FixedAcs(); break;
				case 112: FixedAtn(); break;
				case 113: FixedSqrt(); break;

				case 115: FloatMulAdd(); break; //FMA
				case 116: FloatInRange(); break;
				case 117: FloatSumOfProducts3(); break;
				case 118: FloatNormalizeV3(); break;
				case 119: FloatRaySphereHit(); break;
				#endregion

				case 240: EvaluateExpression(); break;
			}
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
		private void ArgParmError(string origin, string message)
		{
			_errorcode = ELOKEY_ERRORCODE.ARGPARM;
			if (LokeyGlobals.LokeyDebug != null)
			{
				LokeyGlobals.LokeyDebug.DebugOut($"ArgParmError : {origin} : {message}");
			}
			_outqueue.Clear();
		}

		private void RndBytes()
		{
			string srcmethod = $"{nameof(RndBytes)}";
			lock (_lock)
			{
				if (_outqueue.Count < 1)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				byte bytecount = _outqueue.Dequeue();
				if (bytecount == 0) { return; }
				if(bytecount >16) { bytecount=16; }
				_rng.NextBytes(_rndbytes);
				for (int c = 0; c < bytecount; c++)
				{
					_inqueue.Enqueue(_rndbytes[c]);
				}
			}
		}
		private void RndBytesLimited()
		{
			string srcmethod = $"{nameof(RndBytesLimited)}";
			lock (_lock)
			{
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				byte bytecount = _outqueue.Dequeue();
				byte bytelimit = _outqueue.Dequeue();
				if (bytecount == 0) { return; }
				if (bytecount > 16) { bytecount = 16; }
				for (int c = 0; c < bytecount; c++)
				{
					_inqueue.Enqueue((byte)(_rng.Next(0, bytelimit + 1)));
				}
			}
		}

		public void RndFloat()
		{
			string srcmethod = $"{nameof(RndFloat)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var res = _rng.NextDouble();
				ZUT.WriteZXFloatToMemory(_hostram, float1address, res);
			}
		}
		public void RndFixed()
		{
			string srcmethod = $"{nameof(RndFixed)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var res = _rng.NextDouble();
				ZUT.WriteZXFixedToMemory(_hostram, fixed1address, res);
			}
		}

		private void EvaluateExpression()
		{
			string srcmethod= $"{nameof(EvaluateExpression)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				//expression string address, parameter count, parameters base address, destination address
				if (_outqueue.Count < 2+1+2+2)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort expressionstringaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				string expressionstring = ZUT.ReadStringFromMemory(_hostram,expressionstringaddress);
				byte parametercount = _outqueue.Dequeue();
				ushort parameterbaseaddress= ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort outaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);

				//Safe replacement
				//expressionstring = expressionstring.ToLower();
				//expressionstring = expressionstring.Replace("sin(", "Sin(");
				//expressionstring = expressionstring.Replace("cos(", "Cos(");
				//expressionstring = expressionstring.Replace("tan(", "Tan(");
				//expressionstring = expressionstring.Replace("asin(", "Asin(");
				//expressionstring = expressionstring.Replace("acor(", "Acos(");
				//expressionstring = expressionstring.Replace("atan(", "Atan(");
				//expressionstring = expressionstring.Replace("sqr(", "Sqrt(");
				//expressionstring = expressionstring.Replace("sqrt(", "Sqrt(");

				Expression expression=null;
				if (_expressioncache.ContainsKey(expressionstring))
				{
					expression = _expressioncache[expressionstring];
				}
				else
				{
					try
					{
						expression = new Expression(expressionstring);
						_expressioncache.Add(expressionstring, expression);
					}
					catch (Exception ex)
					{
						LokeyGlobals.LokeyDebug.DebugOut($"{srcmethod} : Expression {expressionstring} threw exception on compilation {ex.ToString()}");
						return;
					}
				}

				for (int c = 0; c < parametercount; c++)
				{
					ushort floataddress =ZUT.ReadUIntegerFromMemory(_hostram,(ushort)(parameterbaseaddress + c * 2));
					expression.Parameters[$"par{c.ToString().PadLeft(2, '0')}"]=(ZUT.ReadZXFloatFromMemory(_hostram,floataddress)*1.0);
				}

				double result=0.0;

				try
				{
					result = (double)(expression.Evaluate());
				}
				catch (Exception ex)
				{
					LokeyGlobals.LokeyDebug.DebugOut($"{srcmethod} : Expression {expressionstring} threw exception on evaluation {ex.ToString()}");
				}
				if (double.IsNaN(result))
				{
					LokeyGlobals.LokeyDebug.DebugOut($"{srcmethod} : Expression {expressionstring} evaluated to IsNAN, check your formula for invalid operations/values");
					result = 123456789.123456789;
				}
				ZUT.WriteZXFloatToMemory(_hostram, outaddress, result);
			}
		}
		#region FPU
		public void FloatAdd()
		{
			string srcmethod = $"{nameof(FloatAdd)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, float2address);
				var res = float1 + float2;
				ZUT.WriteZXFloatToMemory(_hostram,float3address, res);
			}
		}
		public void FixedAdd()
		{
			string srcmethod = $"{nameof(FixedAdd)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double float2 = ZUT.ReadZXFixedFromMemory(_hostram, fixed2address);
				var res = float1 + float2;
				ZUT.WriteZXFixedToMemory(_hostram, fixed3address, res);
			}
		}
		public void FloatSub()
		{
			string srcmethod = $"{nameof(FloatSub)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram,float2address);
				var res = float1 - float2;
				ZUT.WriteZXFloatToMemory(_hostram, float3address, res);
			}
		}
		public void FixedSub()
		{
			string srcmethod = $"{nameof(FixedSub)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double float2 = ZUT.ReadZXFloatFromMemory(_hostram,fixed2address);
				var res = float1 - float2;
				ZUT.WriteZXFixedToMemory(_hostram,fixed3address, res);
			}
		}
		public void FloatMul()
		{
			string srcmethod = $"{nameof(FloatMul)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram,float2address);
				var res = float1 * float2;
				ZUT.WriteZXFloatToMemory(_hostram, float3address, res);
			}
		}
		public void FixedMul()
		{
			string srcmethod = $"{nameof(FixedMul)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double float2 = ZUT.ReadZXFloatFromMemory(_hostram,fixed2address);
				var res = float1 * float2;
				ZUT.WriteZXFixedToMemory(_hostram,fixed3address, res);
			}
		}
		public void FloatDiv()
		{
			string srcmethod = $"{nameof(FloatDiv)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram,float2address);
				double res = 0;
				if (float2 != 0) { res = float1 / float2; }
				ZUT.WriteZXFloatToMemory(_hostram, float3address, res);
			}
		}
		public void FixedDiv()
		{
			string srcmethod = $"{nameof(FixedDiv)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double float2 = ZUT.ReadZXFloatFromMemory(_hostram,fixed2address);
				double res = 0;
				if (float2 != 0) { res = float1 / float2; }
				ZUT.WriteZXFixedToMemory(_hostram,fixed3address, res);
			}
		}
		public void FloatPow()
		{
			string srcmethod = $"{nameof(FloatPow)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram,float2address);
				double res = Math.Pow(float1, float2);
				ZUT.WriteZXFloatToMemory(_hostram, float3address, res);
			}
		}
		public void FixedPow()
		{
			string srcmethod = $"{nameof(FixedPow)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double float2 = ZUT.ReadZXFloatFromMemory(_hostram,fixed2address);
				double res = Math.Pow(float1, float2);
				ZUT.WriteZXFixedToMemory(_hostram,fixed3address, res);
			}
		}
		public void FloatExp()
		{
			string srcmethod = $"{nameof(FloatExp)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Exp(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedExp()
		{
			string srcmethod = $"{nameof(FixedExp)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Exp(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatLn()
		{
			string srcmethod = $"{nameof(FloatLn)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Log(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedLn()
		{
			string srcmethod = $"{nameof(FixedLn)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Log(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatSin()
		{
			string srcmethod = $"{nameof(FloatSin)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Sin(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedSin()
		{
			string srcmethod = $"{nameof(FixedSin)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Sin(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatCos()
		{
			string srcmethod = $"{nameof(FloatCos)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Cos(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedCos()
		{
			string srcmethod = $"{nameof(FixedCos)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Cos(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatTan()
		{
			string srcmethod = $"{nameof(FloatTan)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Tan(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedTan()
		{
			string srcmethod = $"{nameof(FixedTan)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Tan(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatAsn()
		{
			string srcmethod = $"{nameof(FloatAsn)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Asin(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedAsn()
		{
			string srcmethod = $"{nameof(FixedAsn)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Asin(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatAcs()
		{
			string srcmethod = $"{nameof(FloatAcs)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Acos(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedAcs()
		{
			string srcmethod = $"{nameof(FixedAcs)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Acos(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatAtn()
		{
			string srcmethod = $"{nameof(FloatAtn)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				double res = Math.Atan(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedAtn()
		{
			string srcmethod = $"{nameof(FixedAtn)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram,fixed1address);
				double res = Math.Atan(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatSqrt()
		{
			string srcmethod = $"{nameof(FloatSqrt)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				double res = Math.Sqrt(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FixedSqrt()
		{
			string srcmethod = $"{nameof(FixedSqrt)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				double float1 = ZUT.ReadZXFixedFromMemory(_hostram, fixed1address);
				double res = Math.Sqrt(float1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FloatAbs()
		{
			string srcmethod = $"{nameof(FloatAbs)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				double res = Math.Abs(float1);
				ZUT.WriteZXFloatToMemory(_hostram, float2address, res);
			}
		}
		public void FloatCompare()
		{
			string srcmethod = $"{nameof(FloatCompare)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort ubyteaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, float2address);
				byte compareresult = 0;
				if (float1 > float2)
				{
					compareresult = 1;
				}
				else
				{
					if (float1 < float2)
					{
						compareresult = 2;
					}
				}
				_hostram[ubyteaddress] = compareresult;
			}
		}
		public void FixedAbs()
		{
			string srcmethod = $"{nameof(FixedAbs)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var fixed1 = ZUT.ReadZXFixedFromMemory(_hostram, fixed1address);
				double res = Math.Abs(fixed1);
				ZUT.WriteZXFixedToMemory(_hostram, fixed2address, res);
			}
		}
		public void FixedCompare()
		{
			string srcmethod = $"{nameof(FixedCompare)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 6)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort fixed1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort fixed2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort ubyteaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var fixed1 = ZUT.ReadZXFixedFromMemory(_hostram, fixed1address);
				var fixed2 = ZUT.ReadZXFixedFromMemory(_hostram, fixed2address);
				byte compareresult = 0;
				if (fixed1 > fixed2)
				{
					compareresult = 1;
				}
				else
				{
					if (fixed1 < fixed2)
					{
						compareresult = 2;
					}
				}
				_hostram[ubyteaddress] = compareresult;
			}
		}
		public void FloatSumOfProducts3()
		{
			//RES=A*B+C*D+E*F
			string srcmethod = $"{nameof(FloatSumOfProducts3)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 14)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float4address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float5address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float6address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort floatdestinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, float2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram, float3address);
				var float4 = ZUT.ReadZXFloatFromMemory(_hostram, float4address);
				var float5 = ZUT.ReadZXFloatFromMemory(_hostram, float5address);
				var float6 = ZUT.ReadZXFloatFromMemory(_hostram, float6address);
				double res = ((float1 * float2) + (float3 * float4) + (float5 * float6));
				ZUT.WriteZXFloatToMemory(_hostram, floatdestinationaddress, res);
			}
		}
		public void FloatNormalizeV3()
		{
			string srcmethod = $"{nameof(FloatNormalizeV3)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				double res = 0;
				ushort V1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort destaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var V1X = ZUT.ReadZXFloatFromMemory(_hostram,V1address);
				var V1Y = ZUT.ReadZXFloatFromMemory(_hostram,(ushort)(V1address + 5));
				var V1Z = ZUT.ReadZXFloatFromMemory(_hostram,(ushort)(V1address + 10));
				float mag = (float)Math.Sqrt(V1X * V1X + V1Y * V1Y + V1Z * V1Z);
				float invLen = (mag > 0.00001f) ? 1.0f / mag : 0.0f;
				res = V1X * invLen;
				ZUT.WriteZXFloatToMemory(_hostram, destaddress, res);
				res = V1Y * invLen;
				ZUT.WriteZXFloatToMemory(_hostram,(ushort)(destaddress + 5), res);
				res = V1Z * invLen;
				ZUT.WriteZXFloatToMemory(_hostram, (ushort)(destaddress + 10), res);
			}
		}
		public void FloatRaySphereHit()
		{
			string srcmethod = $"{nameof(FloatRaySphereHit)}";
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 12)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				double res = 0;
				ushort origPtr = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				Vector3 O = ZUT.ReadVector3FromMemory(_hostram,origPtr);
				ushort dirPtr = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				Vector3 D = ZUT.ReadVector3FromMemory(_hostram,dirPtr);
				ushort sPosPtr = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				Vector3 S = ZUT.ReadVector3FromMemory(_hostram,sPosPtr);
				ushort sRadPtr = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				float R2 = (float)ZUT.ReadZXFloatFromMemory(_hostram,sRadPtr);
				ushort nearTPtr = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort farTPtr = ZUT.ReadUIntegerFromOutQueue(_outqueue);

				Vector3 L = S - O;
				float tca = Vector3.Dot(L, D);

				// Initial check: if tca < 0, the sphere is behind the ray origin
				if (tca < 0)
				{
					ZUT.WriteZXFloatToMemory(_hostram,nearTPtr, -1.0f);
					ZUT.WriteZXFloatToMemory(_hostram, farTPtr, -1.0f);
					return;
				}

				float d2 = Vector3.Dot(L, L) - (tca * tca);
				if (d2 > R2)
				{
					ZUT.WriteZXFloatToMemory(_hostram, nearTPtr, -1.0f);
					ZUT.WriteZXFloatToMemory(_hostram, farTPtr, -1.0f);
					return;
				}

				float thc = (float)Math.Sqrt(R2 - d2);

				// Write results to their independent destinations
				ZUT.WriteZXFloatToMemory(_hostram, nearTPtr, tca - thc);
				ZUT.WriteZXFloatToMemory(_hostram, farTPtr, tca + thc);
			}
		}
		public void FloatMulAdd()
		{
			//A*B+C
			string srcmethod = $"{nameof(FloatMulAdd)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 8)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort floatdestinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram,float2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram,float3address);
				double res = float1 * float2 + float3;
				ZUT.WriteZXFloatToMemory(_hostram, floatdestinationaddress, res);
			}
		}
		public void FloatMulSub()
		{
			//A*B-C
			string srcmethod = $"{nameof(FloatMulSub)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 8)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort floatdestinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, float2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram, float3address);
				double res = float1 * float2 - float3;
				ZUT.WriteZXFloatToMemory(_hostram, floatdestinationaddress, res);
			}
		}
		public void FloatDivAdd()
		{
			//A/B+C
			string srcmethod = $"{nameof(FloatDivAdd)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 8)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort floatdestinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, float2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram, float3address);
				double res = float1 / float2 + float3;
				ZUT.WriteZXFloatToMemory(_hostram, floatdestinationaddress, res);
			}
		}
		public void FloatDivSub()
		{
			//A/B-C
			string srcmethod = $"{nameof(FloatDivSub)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 8)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort floatdestinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, float2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram, float3address);
				double res = float1 / float2 - float3;
				ZUT.WriteZXFloatToMemory(_hostram, floatdestinationaddress, res);
			}
		}
		public void FloatInRange()
		{
			//min<val<max
			string srcmethod = $"{nameof(FloatInRange)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 8)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort float1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort float3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort bytedestinationaddress = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram,float1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram,float2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram,float3address);
				byte outbyte = 0;
				if (float1 > float2 && float1 < float3) { outbyte = 1; } else { outbyte = 0; }
				_hostram[bytedestinationaddress] = outbyte;
			}
		}
		
		public void FloatRotate2D()
		{
			//p4=p1*cos(p3)−p2*sin(p3)
			//p5=p1*sin(p3)+p2*cos(p3)
			string srcmethod = $"{nameof(FloatRotate2D)}";
			_errorcode = ELOKEY_ERRORCODE.NONE;
			lock (_lock)
			{
				if (_outqueue.Count < 10)
				{
					ArgNumError($"{srcmethod}");
					_outqueue.Clear();
					return;
				}
				ushort p1address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort p2address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort p3address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort p4address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				ushort p5address = ZUT.ReadUIntegerFromOutQueue(_outqueue);
				var float1 = ZUT.ReadZXFloatFromMemory(_hostram, p1address);
				var float2 = ZUT.ReadZXFloatFromMemory(_hostram, p2address);
				var float3 = ZUT.ReadZXFloatFromMemory(_hostram, p3address);
				double newx=float1*Math.Cos(float3)-float2*Math.Sin(float3);
				double newy=float1*Math.Sin(float3)+float2*Math.Cos(float3);
				ZUT.WriteZXFloatToMemory(_hostram, p4address,newx);
				ZUT.WriteZXFloatToMemory(_hostram, p5address, newy);
			}
		}
		#endregion

		public void Reset()
		{
		}
		public void RegisterDevice(zx_spectrum hostmachine)
		{
			hostmachine.io_devices.Remove(this);
			hostmachine.io_devices.Add(this);
			_host = hostmachine;
			_hostram = new ZxRam(_host);
		}
		public void UnregisterDevice(zx_spectrum hostmachine)
		{
			hostmachine.io_devices.Remove(this);
			_host = null;
			_hostram = null;
		}

		public void Shutdown()
		{
			LokeyGlobals.LokeyDebug.DeviceHide();
		}
	}
}

