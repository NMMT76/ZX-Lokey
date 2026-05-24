using Speccy;
using Speccy.Devices.ULAZX;
using SpeccyCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Buffer = System.Buffer;
using LUT = LOKEY.LOKEY_Utility_Types;

namespace LOKEY
{
	public partial class LokeyULA : IODevice
	{
		private static ILokeyControllerDevice _ulazxcontroller;
		private const ushort ZXVAR_NMIADD = 23728;
		private const ushort ZXVAR_DEST = 23629;
		private const ushort ZXVAR_WORKSP = 23649;
		private const ushort ZXVAR_ATTR_P = 23693;

		private const ushort ZXMEM_ATTRIBUTE_BASE = 0x5800;

		private const ushort ATTR_P_VM0 = 23693;

		private static Random _rng = new Random(321);

		private int tr, tb, tg;
		private readonly object _lock = new object();

		public byte[] _copper_intensity = new byte[192];
		public byte[] _copper_palette = new byte[192];

		private zx_spectrum _host = null;

		private Queue<byte> _outqueue = new Queue<byte>();
		private Queue<byte> _inqueue = new Queue<byte>();
		public bool Responded { get; set; }

		//Holds the intermediary result of operations before they are copied to _bitmapbuffer
		private readonly byte[] _fillbuffer = new byte[256 * 192]; //6KB

		//Scratch buffer, enough to hold 256*32 for vertical rolls of 32px
		private readonly byte[] _scratchbuffer = new byte[256 * 32]; //1KB, much like other buffers, we using bytes instead of bits for convenience

		const int SCREEN_WIDTH_BYTES = 32;
		const int SCREEN_HEIGHT_PIXELS = 192;
		// Use a full screen temporary buffer to handle wrapping correctly without overwriting.
		// This is more memory intensive but ensures correct wrap-around for vertical roll.
		byte[] _scrollbuffer = new byte[SCREEN_WIDTH_BYTES * SCREEN_HEIGHT_PIXELS];

		//The ULAZX video mode
		//0 - Use standard ZX Spectrum mode - Default
		//1 - 4*4 attribute blocks with 64c extended palette
		public byte VideoMode { get; private set; }

		public const byte ULAZX_FILLOVERDRAW = 0x80;
		public const byte ULAZX_FILLPATTERN = 0x40;
		public const byte ULAZX_FILLPATTERNINDEX = 0x3f;
		public const byte ULAZX_FILLPATTERNINDEXY = 0x38;
		public const byte ULAZX_FILLPATTERNINDEXX = 0x7;

		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

		private ELOKEY_ERRORCODE _errorcode = ELOKEY_ERRORCODE.NONE;

		public SPECTRUM_DEVICE DeviceID { get { return SPECTRUM_DEVICE.ULA_ZX; } }

		private const int _dataport = 129;
		private const int _commandport = 131;

		public LokeyULA()
		{
			BuildRowTable();
			BuildAttrTable();
			var k = CopperPaletteGenerator.IntensityPalettes;
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
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
				case 10: Cls(); break;
				case 11: ClearBitmap(); break;
				case 12: ClearAttributes(); break;

				case 20: Plot(); break;
				case 21: Line(); break;
				case 22: Rectangle(false); break; //Rectangle
				case 23: Rectangle(true); break; //RectangleFilled
				case 24: Triangle(false); break; //Triangle
				case 25: Triangle(true); break; //TriangleFilled
				case 26: Circle(false); break; //Circle
				case 27: Circle(true); break; //CircleFilled

				case 140: Scroll(); break;
				case 141: Roll(); break;

				case 150: SetCopperIntensity(); break;
				case 151: SetCopperIntensityRange(); break;
				case 152: CopperIntensityShift(); break;
				case 153: CopperIntensityRoll(); break;
				case 155: SetCopperPalette(); break;
				case 156: SetCopperPaletteRange(); break;
				case 157: CopperPaletteShift(); break;
				case 158: CopperPaletteRoll(); break;

				case 160: PrintChar(); break;
				case 161: PrintLine(); break;
			}
		}
		public void PrintChar()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(PrintChar)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
					return;
				}
				byte x = _outqueue.Dequeue();
				byte y = _outqueue.Dequeue();
				byte character = _outqueue.Dequeue();
				byte masked = _outqueue.Dequeue();
				if (x > 255 || y > 191) { return; }
				switch (VideoMode)
				{
					case 0:
						if (masked == 0)
						{
							Print(x, y, ((char)character).ToString(), false);
						}
						else
						{
							Print(x, y, ((char)character).ToString(), true);
						}
						break;
					case 1:
						break;
				}
			}
		}
		public void PrintLine()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(PrintChar)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
					return;
				}
				byte x = _outqueue.Dequeue();
				byte y = _outqueue.Dequeue();
				byte character = _outqueue.Dequeue();
				byte masked = _outqueue.Dequeue();
				if (x > 255 || y > 191) { return; }
				switch (VideoMode)
				{
					case 0:
						if (masked == 0)
						{
							Print(x, y, ((char)character).ToString(), false);
						}
						else
						{
							Print(x, y, ((char)character).ToString(), false);
						}
						break;
					case 1:
						break;
				}
			}
		}

		public void Plot()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Plot)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
					return;
				}
				byte x = _outqueue.Dequeue();
				byte y = _outqueue.Dequeue();
				if (x > 255 || y > 191) { return; }
				switch (VideoMode)
				{
					case 0:
						byte attr = LokeyGlobals.HostRAM[ATTR_P_VM0];
						SetPixelAndAttr(_host.screen, x, 191 - y, attr);
						break;
					case 1:
						break;
				}
			}
		}
		public void Line()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 4)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Line)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
					return;
				}
				byte x1 = _outqueue.Dequeue();
				byte y1 = _outqueue.Dequeue();
				byte x2 = _outqueue.Dequeue();
				byte y2 = _outqueue.Dequeue();
				switch (VideoMode)
				{
					case 0:
						LineBLA(x1, y1, x2, y2);
						break;
				}
			}
		}
		private void LineBLA(byte x1, byte y1, byte x2, byte y2)
		{
			int ix1 = x1;
			int iy1 = y1;
			int ix2 = x2;
			int iy2 = y2;
			int dx = Math.Abs(ix2 - ix1);
			int dy = Math.Abs(iy2 - iy1);
			int sx = ix1 < ix2 ? 1 : -1;
			int sy = iy1 < iy2 ? 1 : -1;
			int err = dx - dy;
			while (true)
			{
				if ((uint)ix1 < 256u && (uint)iy1 < 192u)
				{
					byte attr = LokeyGlobals.HostRAM[ATTR_P_VM0];
					SetPixelAndAttr(_host.screen, ix1, 191 - iy1, attr);
				}
				if (ix1 == ix2 && iy1 == iy2) { break; }
				int e2 = err << 1;
				if (e2 > -dy)
				{
					err -= dy;
					ix1 += sx;
				}
				if (e2 < dx)
				{
					err += dx;
					iy1 += sy;
				}
			}
		}
		//Special case for fill buffer
		private void LineBLAFill(byte x1, byte y1, byte x2, byte y2)
		{
			int ix1 = x1;
			int iy1 = y1;
			int ix2 = x2;
			int iy2 = y2;
			int dx = Math.Abs(ix2 - ix1);
			int dy = Math.Abs(iy2 - iy1);
			int sx = ix1 < ix2 ? 1 : -1;
			int sy = iy1 < iy2 ? 1 : -1;
			int err = dx - dy;
			while (true)
			{
				if ((uint)ix1 < 256u && (uint)iy1 < 192u)
				{
					_fillbuffer[iy1 * 256 + ix1] = 1;
				}
				if (ix1 == ix2 && iy1 == iy2) { break; }
				int e2 = err << 1;
				if (e2 > -dy)
				{
					err -= dy;
					ix1 += sx;
				}
				if (e2 < dx)
				{
					err += dx;
					iy1 += sy;
				}
			}
		}
		private void Rectangle(bool filled)
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (filled)
				{
					if (_outqueue.Count < 5)
					{
						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Rectangle)} (Filled) : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
						return;
					}
				}
				else
				{
					if (_outqueue.Count < 4)
					{
						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Rectangle)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
						return;
					}
				}

				byte x1 = _outqueue.Dequeue();
				byte y1 = _outqueue.Dequeue();
				byte x2 = _outqueue.Dequeue();
				byte y2 = _outqueue.Dequeue();
				byte fillbitpattern = 0;
				byte filloverdraw = 0;
				byte fillpattern = 0;
				byte fillindex = 0;
				byte swap;
				if (filled)
				{
					fillbitpattern = _outqueue.Dequeue();
					if (fillbitpattern != 0)
					{
						filloverdraw = (byte)((fillbitpattern & ULAZX_FILLOVERDRAW) >> 7);
						fillpattern = (byte)((fillbitpattern & ULAZX_FILLPATTERN) >> 6);
						fillindex = (byte)(fillbitpattern & ULAZX_FILLPATTERNINDEX);
					}
				}
				if (x1 == x2 && y1 == y2 && y1 < 192)
				{
					SetPixelAndAttr(_host.screen, x1, 191 - y1, LokeyGlobals.HostRAM[ATTR_P_VM0]);
				}
				else
				{
					//Order the points so that x1<x2 and y1<y2 to simplify operations
					if (x1 > x2) { swap = x1; x1 = x2; x2 = swap; }
					if (y1 > y2) { swap = y1; y1 = y2; y2 = swap; }

					ClearPolyBuffer();
					LineBLA(x1, y1, x2, y1);
					LineBLA(x2, y1, x2, y2);
					LineBLA(x2, y2, x1, y2);
					LineBLA(x1, y2, x1, y1);
					if (filled)
					{
						LineBLAFill(x1, y1, x2, y1);
						LineBLAFill(x2, y1, x2, y2);
						LineBLAFill(x2, y2, x1, y2);
						LineBLAFill(x1, y2, x1, y1);
						//Rectangle degeneracy check
						if (!((x2 - x1) < 2 || (y2 - y1) < 2))
						{
							//On rectangles we don't need the centroid, x1+1,y1+1 solves all cases
							ScanlineFloodFill((byte)(x1 + 1), (byte)(y1 + 1), _fillbuffer);
						}
					}
					if (fillpattern == 1)
					{
						//PatternFill(fillindex, _fillbuffer);
					}
					if (filloverdraw == 1)
					{
						LineBLAFill(x1, y1, x2, y1);
						LineBLAFill(x2, y1, x2, y2);
						LineBLAFill(x2, y2, x1, y2);
						LineBLAFill(x1, y2, x1, y1);
					}
					CopyPolyToBack();
				}
			}
		}

		public void Circle(bool filled)
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 3) { _errorcode = ELOKEY_ERRORCODE.ARGNUM; return; }
				byte x = _outqueue.Dequeue();
				byte y = _outqueue.Dequeue();
				byte r = _outqueue.Dequeue();
				byte fillbitpattern = 0;
				byte filloverdraw = 0;
				byte fillpattern = 0;
				byte fillindex = 0;
				if (filled)
				{
					fillbitpattern = _outqueue.Dequeue();
					if (fillbitpattern != 0)
					{
						filloverdraw = (byte)((fillbitpattern & ULAZX_FILLOVERDRAW) >> 7);
						fillpattern = (byte)((fillbitpattern & ULAZX_FILLPATTERN) >> 6);
						fillindex = (byte)(fillbitpattern & ULAZX_FILLPATTERNINDEX);
					}
				}
				if (r == 0) { return; }
				if (r == 1)
				{
					if (y < 192)
					{
						SetPixelAndAttr(_host.screen, x, 191 - y, LokeyGlobals.HostRAM[ATTR_P_VM0]);
					}
					return;
				}
				ClearPolyBuffer();
				CircleMCA(x, y, r);
				if (filled)
				{
					CircleMCAFill(x, y, r);
					ScanlineFloodFill(x, y, _fillbuffer);
				}
				if (fillpattern == 1)
				{
					//PatternFill(fillindex, _fillbuffer);
				}
				if (filloverdraw == 1)
				{
					CircleMCAFill(x, y, r);
				}
				CopyPolyToBack();
			}
		}
		public void CircleMCA(byte x, byte y, byte r)
		{
			long work = 0;
			int cx = x;
			int cy = y;
			int radius = r;

			int dx = radius;
			int dy = 0;
			int decision = 1 - radius;

			while (dx >= dy)
			{
				SetCirclePoint(cx + dx, cy + dy); work++;
				SetCirclePoint(cx + dy, cy + dx); work++;
				SetCirclePoint(cx - dy, cy + dx); work++;
				SetCirclePoint(cx - dx, cy + dy); work++;
				SetCirclePoint(cx - dx, cy - dy); work++;
				SetCirclePoint(cx - dy, cy - dx); work++;
				SetCirclePoint(cx + dy, cy - dx); work++;
				SetCirclePoint(cx + dx, cy - dy); work++;

				dy++;

				if (decision <= 0)
				{
					decision += (2 * dy) + 1;
				}
				else
				{
					dx--;
					decision += (2 * (dy - dx)) + 1;
				}
			}
		}
		public void CircleMCAFill(byte x, byte y, byte r)
		{
			long work = 0;
			int cx = x;
			int cy = y;
			int radius = r;

			int dx = radius;
			int dy = 0;
			int decision = 1 - radius;

			while (dx >= dy)
			{
				SetCirclePointFill(cx + dx, cy + dy); work++;
				SetCirclePointFill(cx + dy, cy + dx); work++;
				SetCirclePointFill(cx - dy, cy + dx); work++;
				SetCirclePointFill(cx - dx, cy + dy); work++;
				SetCirclePointFill(cx - dx, cy - dy); work++;
				SetCirclePointFill(cx - dy, cy - dx); work++;
				SetCirclePointFill(cx + dy, cy - dx); work++;
				SetCirclePointFill(cx + dx, cy - dy); work++;

				dy++;

				if (decision <= 0)
				{
					decision += (2 * dy) + 1;
				}
				else
				{
					dx--;
					decision += (2 * (dy - dx)) + 1;
				}
			}
		}

		private void SetCirclePoint(int x, int y)
		{
			if ((uint)x >= 256u || (uint)y >= 192u) { return; }
			SetPixelAndAttr(_host.screen, x, 191 - y, LokeyGlobals.HostRAM[ATTR_P_VM0]);
		}
		private void SetCirclePointFill(int x, int y)
		{
			if ((uint)x >= 256u || (uint)y >= 192u) { return; }
			_fillbuffer[y * 256 + x] = 1;
		}

		private void Triangle(bool filled)
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (filled)
				{
					if (_outqueue.Count < 7)
					{
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Triangle)} (Filled) : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
						return;
					}
				}
				else
				{
					if (_outqueue.Count < 6)
					{
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Triangle)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM");
						_errorcode = ELOKEY_ERRORCODE.ARGNUM;
						return;
					}
				}
				List<PointByte2D> points = new List<PointByte2D>();
				byte fillbitpattern = 0;
				byte filloverdraw = 0;
				byte fillpattern = 0;
				byte fillindex = 0;
				for (int c = 0; c < 3; c++)
				{
					PointByte2D temp = new PointByte2D(_outqueue.Dequeue(), _outqueue.Dequeue());
					points.Add(temp);
				}
				if (filled)
				{
					fillbitpattern = _outqueue.Dequeue();
					if (fillbitpattern != 0)
					{
						filloverdraw = (byte)((fillbitpattern & ULAZX_FILLOVERDRAW) >> 7);
						fillpattern = (byte)((fillbitpattern & ULAZX_FILLPATTERN) >> 6);
						fillindex = (byte)(fillbitpattern & ULAZX_FILLPATTERNINDEX);
					}
				}
				ClearPolyBuffer();
				LineBLA(points[0].X, points[0].Y, points[1].X, points[1].Y);
				LineBLA(points[1].X, points[1].Y, points[2].X, points[2].Y);
				LineBLA(points[2].X, points[2].Y, points[0].X, points[0].Y);
				if (filled)
				{
					LineBLAFill(points[0].X, points[0].Y, points[1].X, points[1].Y);
					LineBLAFill(points[1].X, points[1].Y, points[2].X, points[2].Y);
					LineBLAFill(points[2].X, points[2].Y, points[0].X, points[0].Y);
					//Check for degeneracy before fill
					if (!IsDegenerateTriangle(points))
					{
						double xc, yc;
						//Centroid of a triangle (specific version)
						xc = (points[0].X + points[1].X + points[2].X) / 3.0;
						yc = (points[0].Y + points[1].Y + points[2].Y) / 3.0;
						ScanlineFloodFill((byte)xc, (byte)yc, _fillbuffer);
					}
				}
				if (fillpattern == 1)
				{
					//PatternFill(fillindex, _fillbuffer);
				}
				if (filloverdraw == 1)
				{
					LineBLAFill(points[0].X, points[0].Y, points[1].X, points[1].Y);
					LineBLAFill(points[1].X, points[1].Y, points[2].X, points[2].Y);
					LineBLAFill(points[2].X, points[2].Y, points[0].X, points[0].Y);
				}
				CopyPolyToBack();
			}
		}
		public static bool IsDegenerateTriangle(List<PointByte2D> points)
		{
			int areaTwice = points[0].X * (points[1].Y - points[2].Y)
						  + points[1].X * (points[2].Y - points[0].Y)
						  + points[2].X * (points[0].Y - points[1].Y);
			return areaTwice == 0;
		}
		public static long TriangleArea(int x1, int y1, int x2, int y2, int x3, int y3)
		{
			return (long)(Math.Abs((x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2)) / 2.0));
		}

		public void Print(int x, int y, string line, bool masked)
		{

			// CHARS variable is at 23606 and 23607
			// It's a 16-bit address, little-endian
			int fontAddress = LokeyGlobals.HostRAM[23606] | (LokeyGlobals.HostRAM[23607] << 8);
			byte currentAttr = LokeyGlobals.HostRAM[ATTR_P_VM0];

			foreach (char c in line)
			{
				// Each character is 8 bytes in the font definition
				int charOffset = (c & 0xFF) * 8;
				int currentFontAddress = fontAddress + charOffset;

				for (int row = 0; row < 8; row++)
				{
					int pixelY = y + row;
					if (pixelY >= 192) // Screen height is 192 pixels
					{
						break;
					}

					byte fontByte = LokeyGlobals.HostRAM[currentFontAddress + row];

					for (int bit = 0; bit < 8; bit++)
					{
						int pixelX = x + bit;
						if (pixelX >= 256) // Screen width is 256 pixels
						{
							break;
						}

						bool fontPixelSet = ((fontByte >> (7 - bit)) & 1) == 1;

						if (fontPixelSet)
						{
							SetPixelAndAttr(_host.screen, pixelX, pixelY, currentAttr);
						}
						else if (!masked)
						{
							ClearPixel(_host.screen, pixelX, pixelY);
						}
					}
				}
				x += 8; // Move to the next character position (8 pixels wide)
			}
		}

		private void ArgNumError(string origin)
		{
			_errorcode = ELOKEY_ERRORCODE.ARGNUM;
			LokeyGlobals.LokeyDebug.DebugOut($"ArgNumError : {origin} : {_outqueue.Count}bytes");
			_outqueue.Clear();
		}
		private void ArgParmError(string origin, string message)
		{
			_errorcode = ELOKEY_ERRORCODE.ARGPARM;
			LokeyGlobals.LokeyDebug.DebugOut($"ArgParmError : {origin} : {message}");
			_outqueue.Clear();
		}

		// Buffer for a single line of video memory (32 bytes).
		// This is a class field because it's re-used across calls to avoid temporary allocations.
		private readonly byte[] _lineBuffer = new byte[32];

		/// <summary>
		/// Scrolls/Rolls the video memory of the ZX Spectrum by the specified pixels in any direction.
		/// Reads 3 bytes from the _outqueue:
		/// 1. Direction (byte): 0: Up, 1: Down, 2: Left, 3: Right, 4: Up-Left, 5: Up-Right, 6: Down-Left, 7: Down-Right
		/// 2. Pixels (byte): 1, 2, 4, 8, 16, 32, 64
		/// 3. FillValue (byte): The value used for newly exposed pixels.
		/// </summary>
		public void Scroll()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 3)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Scroll)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM - Requires direction, pixels, and fillValue.");
					return;
				}

				ScrollDirection direction = (ScrollDirection)_outqueue.Dequeue();
				byte pixels = _outqueue.Dequeue();
				byte attributes = _outqueue.Dequeue();

				if (!IsValidScrollPixels(pixels))
				{
					_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Scroll)} : EULAZX_ERRORCODE : ULAZXEC_ARG - Invalid pixels value. Must be 1, 2, 4, 8, 16, 32, or 64.");
					return;
				}

				switch (VideoMode)
				{
					case 0: // ZX Spectrum 256x192 16-color bitmap mode
						PerformScrollVM0(direction, pixels);
						break;
					case 1:
						// Placeholder for other video modes if they become relevant.
						_errorcode = ELOKEY_ERRORCODE.ARGVALUE; // Or a specific error for unsupported mode operation
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Scroll)} : EULAZX_ERRORCODE : ULAZXEC_ARG - VideoMode 1 scrolling not implemented.");
						break;
					default:
						_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Scroll)} : EULAZX_ERRORCODE : ULAZXEC_ARG - Unsupported VideoMode: {VideoMode}.");
						break;
				}
			}
		}

		/// <summary>
		/// Checks if the provided pixel value is valid for scrolling.
		/// </summary>
		/// <param name="pixels">The number of pixels to scroll.</param>
		/// <returns>True if the pixel value is one of the allowed values, false otherwise.</returns>
		private bool IsValidScrollPixels(byte pixels)
		{
			return pixels == 1 || pixels == 2 || pixels == 4 || pixels == 8 ||
				   pixels == 16 || pixels == 32 || pixels == 64;
		}

		/// <summary>
		/// Performs the actual scrolling operation for VideoMode 0 (ZX Spectrum 256x192).
		/// Handles all 8 directions (Up, Down, Left, Right, and diagonals).
		/// </summary>
		/// <param name="direction">The direction of the scroll (0-7).</param>
		/// <param name="pixels">The number of pixels to scroll.</param>
		/// <param name="fillValue">The byte value to fill newly exposed pixel areas.</param>
		private void PerformScrollVM0(ScrollDirection direction, byte pixels)
		{
			// For diagonal scrolls, we call the horizontal and vertical scrolls sequentially.
			// The order (vertical then horizontal, or vice versa) doesn't change the final
			// bitmap state for a simple shift.
			switch (direction)
			{
				case ScrollDirection.Up: // Up
					ScrollVerticalVM0(pixels, true);
					break;
				case ScrollDirection.Down: // Down
					ScrollVerticalVM0(pixels, false);
					break;
				case ScrollDirection.Left: // Left
					ScrollHorizontalVM0(pixels, true);
					break;
				case ScrollDirection.Right: // Right
					ScrollHorizontalVM0(pixels, false);
					break;
				case ScrollDirection.UpLeft: // Up-Left
					ScrollVerticalVM0(pixels, true);
					ScrollHorizontalVM0(pixels, true);
					break;
				case ScrollDirection.UpRight: // Up-Right
					ScrollVerticalVM0(pixels, true);
					ScrollHorizontalVM0(pixels, false);
					break;
				case ScrollDirection.DownLeft: // Down-Left
					ScrollVerticalVM0(pixels, false);
					ScrollHorizontalVM0(pixels, true);
					break;
				case ScrollDirection.DownRight: // Down-Right
					ScrollVerticalVM0(pixels, false);
					ScrollHorizontalVM0(pixels, false);
					break;
				default:
					_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(PerformScrollVM0)} : EULAZX_ERRORCODE : ULAZXEC_ARG - Invalid scroll direction: {direction}.");
					break;
			}
		}

		/// <summary>
		/// Performs vertical scrolling on the ZX Spectrum's bitmap display for VideoMode 0.
		/// This method directly copies 32-byte pixel rows using the `ZxFastPixels.rowTable`
		/// to account for the interleaved memory layout. Attribute memory is not touched.
		/// </summary>
		/// <param name="pixels">The number of pixel lines to scroll (1-191).</param>
		/// <param name="up">True for upward scroll, false for downward scroll.</param>
		/// <param name="fillValue">The byte value to fill newly exposed pixel areas.</param>
		private void ScrollVerticalVM0(byte pixels, bool up)
		{
			if (pixels == 0) { return; }
			// Cap pixels to screen height to ensure valid rowTable indexing and avoid unnecessary operations.
			if (pixels > 191) { pixels = 191; }

			byte[] screen = _host.screen;

			if (up)
			{
				// Iterate forwards: copy data from a lower line to a higher line.
				// New lines appear at the bottom.
				for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
				{
					ushort destRowBase = rowTable[y];
					int destAddr = destRowBase;

					if (y + pixels < SCREEN_HEIGHT_PIXELS)
					{
						// Source line is within the screen bounds, copy its data.
						ushort srcRowBase = rowTable[y + pixels];
						int srcAddr = srcRowBase;
						Buffer.BlockCopy(screen, srcAddr, screen, destAddr, SCREEN_WIDTH_BYTES);
					}
					else
					{
						// Source line is outside the screen bounds (newly exposed area).
						// Clear the destination line with the specified fillValue.
						for (int i = 0; i < SCREEN_WIDTH_BYTES; i++) { screen[destAddr + i] = 0; }
					}
				}
			}
			else // Down
			{
				// Iterate backwards: copy data from a higher line to a lower line.
				// New lines appear at the top.
				for (int y = SCREEN_HEIGHT_PIXELS - 1; y >= 0; y--)
				{
					ushort destRowBase = rowTable[y];
					int destAddr = destRowBase;

					if (y - pixels >= 0)
					{
						// Source line is within the screen bounds, copy its data.
						ushort srcRowBase = rowTable[y - pixels];
						int srcAddr = srcRowBase;
						Buffer.BlockCopy(screen, srcAddr, screen, destAddr, SCREEN_WIDTH_BYTES);
					}
					else
					{
						// Source line is outside the screen bounds (newly exposed area).
						// Clear the destination line with the specified fillValue.
						for (int i = 0; i < SCREEN_WIDTH_BYTES; i++) { screen[destAddr + i] = 0; }
					}
				}
			}
		}

		/// <summary>
		/// Performs horizontal scrolling on the ZX Spectrum's bitmap display for VideoMode 0.
		/// This involves bit shifting with carry across bytes for each pixel row. Attribute memory is not touched.
		/// </summary>
		/// <param name="pixels">The number of pixels to scroll (1-7, as shifts of 8 or more are handled by byte shifts).</param>
		/// <param name="left">True for leftward scroll, false for rightward scroll.</param>
		/// <param name="fillValue">The byte value to fill newly exposed pixel areas (on the edge of the screen).</param>
		private void ScrollHorizontalVM0(byte pixels, bool left)
		{
			if (pixels == 0) { return; }
			// Horizontal pixel scroll operates on bits within bytes.
			// A shift of 8 pixels is equivalent to a full byte shift.
			// We only need to handle shifts 1-7; any value >= 8 will effectively wrap.
			if (pixels >= 8) { pixels %= 8; }
			if (pixels == 0) { return; } // After modulo, if pixels became 0, no shift needed.

			byte[] screen = _host.screen;

			if (left)
			{
				// Scroll pixel data left
				for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
				{
					ushort rowBase = rowTable[y];
					int pixelRowStart = rowBase;

					// Copy the current row to the temporary buffer.
					// This is crucial to avoid reading already shifted data during bit manipulation.
					Buffer.BlockCopy(screen, pixelRowStart, _lineBuffer, 0, SCREEN_WIDTH_BYTES);

					for (int x = 0; x < SCREEN_WIDTH_BYTES; x++)
					{
						byte currentByte = _lineBuffer[x];
						// If there's a next byte, get its value; otherwise, it's the screen edge, fill with fillValue.
						byte nextByte = (x + 1 < SCREEN_WIDTH_BYTES) ? _lineBuffer[x + 1] : (byte)0;

						// Shift current byte left by 'pixels', and bring in (8 - pixels) bits from the next byte.
						byte newByte = (byte)(currentByte << pixels);
						newByte |= (byte)(nextByte >> (8 - pixels));

						screen[pixelRowStart + x] = newByte;
					}
				}
			}
			else // Right
			{
				// Scroll pixel data right
				for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
				{
					ushort rowBase = rowTable[y];
					int pixelRowStart = rowBase;

					// Copy the current row to the temporary buffer.
					Buffer.BlockCopy(screen, pixelRowStart, _lineBuffer, 0, SCREEN_WIDTH_BYTES);

					// Iterate backwards to ensure correct carry operations from previous bytes.
					for (int x = SCREEN_WIDTH_BYTES - 1; x >= 0; x--)
					{
						byte currentByte = _lineBuffer[x];
						// If there's a previous byte, get its value; otherwise, it's the screen edge, fill with fillValue.
						byte prevByte = (x - 1 >= 0) ? _lineBuffer[x - 1] : (byte)0;

						// Shift current byte right by 'pixels', and bring in (8 - pixels) bits from the previous byte.
						byte newByte = (byte)(currentByte >> pixels);
						newByte |= (byte)(prevByte << (8 - pixels));

						screen[pixelRowStart + x] = newByte;
					}
				}
			}
		}
		/// <summary>
		/// Rolls the video memory of the ZX Spectrum by the specified pixels in any direction, wrapping content around.
		/// Reads 2 bytes from the _outqueue:
		/// 1. Direction (byte): 0: Up, 1: Down, 2: Left, 3: Right, 4: Up-Left, 5: Up-Right, 6: Down-Left, 7: Down-Right
		/// 2. Pixels (byte): 1, 2, 4, 8, 16, 32, 64
		/// </summary>
		public void Roll()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 3)
				{
					_errorcode = ELOKEY_ERRORCODE.ARGNUM;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Roll)} : EULAZX_ERRORCODE : ULAZXEC_ARGNUM - Requires direction and pixels.");
					return;
				}

				ScrollDirection direction = (ScrollDirection)_outqueue.Dequeue();
				byte pixels = _outqueue.Dequeue();
				byte attributes = _outqueue.Dequeue();

				if (!IsValidScrollPixels(pixels)) // Re-using the same pixel validation
				{
					_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Roll)} : EULAZX_ERRORCODE : ULAZXEC_ARG - Invalid pixels value. Must be 1, 2, 4, 8, 16, 32, or 64.");
					return;
				}

				switch (VideoMode)
				{
					case 0: // ZX Spectrum 256x192 16-color bitmap mode
						PerformRollVM0(direction, pixels);
						break;
					case 1:
						_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Roll)} : EULAZX_ERRORCODE : ULAZXEC_ARG - VideoMode 1 rolling not implemented.");
						break;
					default:
						_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
						LokeyGlobals.LokeyDebug.DebugOut($"{nameof(Roll)} : EULAZX_ERRORCODE : ULAZXEC_ARG - Unsupported VideoMode: {VideoMode}.");
						break;
				}
			}
		}

		/// <summary>
		/// Performs the actual rolling operation for VideoMode 0 (ZX Spectrum 256x192).
		/// Handles all 8 directions (Up, Down, Left, Right, and diagonals).
		/// </summary>
		/// <param name="direction">The direction of the roll (0-7).</param>
		/// <param name="pixels">The number of pixels to roll.</param>
		private void PerformRollVM0(ScrollDirection direction, byte pixels)
		{
			// Similar to Scroll, diagonal rolls call horizontal and vertical rolls.
			switch (direction)
			{
				case ScrollDirection.Up:
					RollVerticalVM0(pixels, true);
					break;
				case ScrollDirection.Down:
					RollVerticalVM0(pixels, false);
					break;
				case ScrollDirection.Left:
					RollHorizontalVM0(pixels, true);
					break;
				case ScrollDirection.Right:
					RollHorizontalVM0(pixels, false);
					break;
				case ScrollDirection.UpLeft:
					RollVerticalVM0(pixels, true);
					RollHorizontalVM0(pixels, true);
					break;
				case ScrollDirection.UpRight:
					RollVerticalVM0(pixels, true);
					RollHorizontalVM0(pixels, false);
					break;
				case ScrollDirection.DownLeft:
					RollVerticalVM0(pixels, false);
					RollHorizontalVM0(pixels, true);
					break;
				case ScrollDirection.DownRight:
					RollVerticalVM0(pixels, false);
					RollHorizontalVM0(pixels, false);
					break;
				default:
					_errorcode = ELOKEY_ERRORCODE.ARGVALUE;
					LokeyGlobals.LokeyDebug.DebugOut($"{nameof(PerformRollVM0)} : EULAZX_ERRORCODE : ULAZXEC_ARG - Invalid roll direction: {direction}.");
					break;
			}
		}

		/// <summary>
		/// Performs vertical rolling on the ZX Spectrum's bitmap display for VideoMode 0, wrapping content.
		/// </summary>
		/// <param name="pixels">The number of pixel lines to roll (1-191).</param>
		/// <param name="up">True for upward roll, false for downward roll.</param>
		private void RollVerticalVM0(byte pixels, bool up)
		{
			if (pixels == 0) { return; }
			if (pixels > 191) { pixels = (byte)(pixels % 192); } // Wrap pixels if larger than screen height
			if (pixels == 0) { return; } // After modulo, if pixels became 0, no roll needed.

			byte[] screen = _host.screen;

			// Copy current screen content to temp buffer
			for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
			{
				ushort srcRowBase = rowTable[y];
				Buffer.BlockCopy(screen, srcRowBase, _scrollbuffer, y * SCREEN_WIDTH_BYTES, SCREEN_WIDTH_BYTES);
			}

			if (up)
			{
				for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
				{
					ushort destRowBase = rowTable[y];
					int srcY = (y + pixels) % SCREEN_HEIGHT_PIXELS; // Calculate wrapped source row
					Buffer.BlockCopy(_scrollbuffer, srcY * SCREEN_WIDTH_BYTES, screen, destRowBase, SCREEN_WIDTH_BYTES);
				}
			}
			else // Down
			{
				for (int y = SCREEN_HEIGHT_PIXELS - 1; y >= 0; y--)
				{
					ushort destRowBase = rowTable[y];
					int srcY = (y - pixels + SCREEN_HEIGHT_PIXELS) % SCREEN_HEIGHT_PIXELS; // Calculate wrapped source row
					Buffer.BlockCopy(_scrollbuffer, srcY * SCREEN_WIDTH_BYTES, screen, destRowBase, SCREEN_WIDTH_BYTES);
				}
			}
		}

		/// <summary>
		/// Performs horizontal rolling on the ZX Spectrum's bitmap display for VideoMode 0, wrapping content.
		/// This involves bit shifting with carry across bytes for each pixel row, with wrapping from one edge to the other.
		/// </summary>
		/// <param name="pixels">The number of pixels to roll (1-7 for bit shifts).</param>
		/// <param name="left">True for leftward roll, false for rightward roll.</param>
		private void RollHorizontalVM0(byte pixels, bool left)
		{
			if (pixels == 0) { return; }
			if (pixels >= 8) { pixels = (byte)(pixels % 8); } // Horizontal pixel roll only applies within an 8-pixel byte boundary
			if (pixels == 0) { return; } // After modulo, if pixels became 0, no shift needed.

			byte[] screen = _host.screen;

			// _lineBuffer is already a class field and will be used for each row.
			byte[] tempRowBuffer = new byte[SCREEN_WIDTH_BYTES]; // Temporary buffer for source row

			if (left)
			{
				for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
				{
					ushort rowBase = rowTable[y];
					int pixelRowStart = rowBase;

					// Copy the current row to a temporary buffer to get original data for shifting
					Buffer.BlockCopy(screen, pixelRowStart, tempRowBuffer, 0, SCREEN_WIDTH_BYTES);

					for (int x = 0; x < SCREEN_WIDTH_BYTES; x++)
					{
						byte currentByte = tempRowBuffer[x];
						byte nextByte;

						if (x + 1 < SCREEN_WIDTH_BYTES)
						{
							nextByte = tempRowBuffer[x + 1];
						}
						else
						{
							// Wrap around: get the byte from the beginning of the row
							nextByte = tempRowBuffer[0];
						}

						byte newByte = (byte)(currentByte << pixels);
						newByte |= (byte)(nextByte >> (8 - pixels));

						screen[pixelRowStart + x] = newByte;
					}
				}
			}
			else // Right
			{
				for (int y = 0; y < SCREEN_HEIGHT_PIXELS; y++)
				{
					ushort rowBase = rowTable[y];
					int pixelRowStart = rowBase;

					// Copy the current row to a temporary buffer to get original data for shifting
					Buffer.BlockCopy(screen, pixelRowStart, tempRowBuffer, 0, SCREEN_WIDTH_BYTES);

					for (int x = SCREEN_WIDTH_BYTES - 1; x >= 0; x--)
					{
						byte currentByte = tempRowBuffer[x];
						byte prevByte;

						if (x - 1 >= 0)
						{
							prevByte = tempRowBuffer[x - 1];
						}
						else
						{
							// Wrap around: get the byte from the end of the row
							prevByte = tempRowBuffer[SCREEN_WIDTH_BYTES - 1];
						}

						byte newByte = (byte)(currentByte >> pixels);
						newByte |= (byte)(prevByte << (8 - pixels));

						screen[pixelRowStart + x] = newByte;
					}
				}
			}
		}

		private void SetCopperIntensity()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{nameof(SetCopperIntensity)}");
					return;
				}
				byte index = _outqueue.Dequeue();
				byte value = _outqueue.Dequeue();
				_copper_intensity[index] = value;
			}
		}
		private void SetCopperIntensityRange()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 3)
				{
					ArgNumError($"{nameof(SetCopperIntensityRange)}");
					return;
				}
				byte start = _outqueue.Dequeue();
				byte end = _outqueue.Dequeue();
				byte value = _outqueue.Dequeue();
				if (start > 191) { start = 191; }
				if (end > 191) { start = 191; }
				for (int c = start; c < end; c++)
				{
					_copper_intensity[c] = value;
				}
			}
		}
		private void CopperIntensityShift()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{nameof(CopperIntensityShift)}");
					return;
				}
				sbyte amount = (sbyte)_outqueue.Dequeue();
				byte fillvalue = _outqueue.Dequeue();
				if (amount == 0) { return; }
				if (amount > 96) { amount = 96; }
				if (amount < -96) { amount = -96; }
				ShiftOrRollInPlace(_copper_intensity, amount, false, fillvalue);
			}
		}
		private void CopperIntensityRoll()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 1)
				{
					ArgNumError($"{nameof(CopperIntensityRoll)}");
					return;
				}
				sbyte amount = (sbyte)_outqueue.Dequeue();
				if (amount == 0) { return; }
				if (amount > 96) { amount = 96; }
				if (amount < -96) { amount = -96; }
				ShiftOrRollInPlace(_copper_intensity, amount, true);
			}
		}

		private void SetCopperPalette()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{nameof(SetCopperPalette)}");
					return;
				}
				byte index = _outqueue.Dequeue();
				byte value = _outqueue.Dequeue();
				if (value > 7) { value = 7; }
				_copper_palette[index] = value;
			}
		}
		private void SetCopperPaletteRange()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 3)
				{
					ArgNumError($"{nameof(SetCopperPaletteRange)}");
					return;
				}
				byte start = _outqueue.Dequeue();
				byte end = _outqueue.Dequeue();
				byte value = _outqueue.Dequeue();
				if (start > 191) { start = 191; }
				if (end > 191) { start = 191; }
				if (value > 7) { value = 7; }
				for (int c = start; c < end; c++)
				{
					_copper_palette[c] = value;
				}
			}
		}
		private void CopperPaletteShift()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 2)
				{
					ArgNumError($"{nameof(CopperPaletteShift)}");
					return;
				}
				sbyte amount = (sbyte)_outqueue.Dequeue();
				byte fillvalue = _outqueue.Dequeue();
				if (amount == 0) { return; }
				if (amount > 96) { amount = 96; }
				if (amount < -96) { amount = -96; }
				if (fillvalue > 7) { fillvalue = 7; }
				ShiftOrRollInPlace(_copper_palette, amount, false, fillvalue);
			}
		}
		private void CopperPaletteRoll()
		{
			lock (_lock)
			{
				_errorcode = ELOKEY_ERRORCODE.NONE;
				if (_outqueue.Count < 1)
				{
					ArgNumError($"{nameof(CopperPaletteRoll)}");
					return;
				}
				sbyte amount = (sbyte)_outqueue.Dequeue();
				if (amount == 0) { return; }
				if (amount > 96) { amount = 96; }
				if (amount < -96) { amount = -96; }
				ShiftOrRollInPlace(_copper_palette, amount, true);
			}
		}

		public static void ShiftOrRollInPlace(byte[] data, int positions, bool wrap, byte fillValue = 0)
		{
			if (data == null || data.Length == 0)
				return;

			int len = data.Length;

			// Normalize to [-len, len)
			int shift = positions % len;

			if (shift == 0)
				return;

			if (wrap)
			{
				// --- In-place rotation using reversal algorithm ---
				// Convert negative shift (left) into equivalent right shift
				if (shift < 0)
					shift += len;

				Reverse(data, 0, len - 1);
				Reverse(data, 0, shift - 1);
				Reverse(data, shift, len - 1);
			}
			else
			{
				// --- Linear shift (no wrap) ---
				if (shift > 0)
				{
					// Right shift
					int move = len - shift;

					if (move > 0)
						Array.Copy(data, 0, data, shift, move);

					// Fill leading gap
					for (int i = 0; i < Math.Min(shift, len); i++)
						data[i] = fillValue;
				}
				else // shift < 0
				{
					int left = -shift;
					int move = len - left;

					if (move > 0)
						Array.Copy(data, left, data, 0, move);

					// Fill trailing gap
					for (int i = move; i < len; i++)
						data[i] = fillValue;
				}
			}

		}

		private static void Reverse(byte[] arr, int start, int end)
		{
			while (start < end)
			{
				byte tmp = arr[start];
				arr[start++] = arr[end];
				arr[end--] = tmp;
			}
		}

		private void ClearPolyBuffer()
		{
			for (int y = 0; y < 192; y++)
			{
				for (int x = 0; x < 256; x++)
				{
					_fillbuffer[y * 256 + x] = 0;
				}
			}
		}

		private void CopyPolyToBack()
		{
			for (int y = 0; y < 192; y++)
			{
				for (int x = 0; x < 256; x++)
				{
					if (_fillbuffer[y * 256 + x] == 1)
					{
						SetPixelAndAttr(_host.screen, x, 191 - y, LokeyGlobals.HostRAM[ATTR_P_VM0]);
					}
					else
					{
						if (_fillbuffer[y * 256 + x] == 2)
						{
							ClearPixel(_host.screen, x, 191 - y);
						}
					}
				}
			}
		}
		public void ScanlineFloodFill(byte x, byte y, byte[] buffer)
		{
			if (buffer == null) { throw new ArgumentNullException(nameof(buffer)); }

			int startX = x;
			int startY = y;

			if ((uint)startX >= 256u || (uint)startY >= 192u) { return; }
			if (buffer[startY * 256 + startX] != 0) { return; }

			Stack<int> stack = new Stack<int>();

			stack.Push(startX);
			stack.Push(startY);

			while (stack.Count > 0)
			{
				int cy = stack.Pop();
				int cx = stack.Pop();

				int left = cx;
				int right = cx;

				while (left > 0 && buffer[cy * 256 + left - 1] == 0) { left--; }
				while (right < 255 && buffer[cy * 256 + right + 1] == 0) { right++; }

				for (int ix = left; ix <= right; ix++)
				{
					buffer[cy * 256 + ix] = 1;
				}

				if (cy > 0)
				{
					ScanAndPush(left, right, cy - 1, buffer, stack);
				}

				if (cy < 191)
				{
					ScanAndPush(left, right, cy + 1, buffer, stack);
				}
			}
		}
		private void ScanAndPush(int left, int right, int y, byte[] buffer, Stack<int> stack)
		{
			int x = left;

			while (x <= right)
			{
				while (x <= right && buffer[y * 256 + x] != 0) { x++; }

				if (x > right) { return; }

				int startX = x;

				while (x <= right && buffer[y * 256 + x] == 0) { x++; }

				stack.Push(startX);
				stack.Push(y);
			}
		}
		public void GetCentroid(IList<PointByte2D> vertices, out double centerX, out double centerY)
		{
			if (vertices == null) { throw new ArgumentNullException(nameof(vertices)); }
			if (vertices.Count < 3) { throw new ArgumentException("A polygon must have at least 3 vertices."); }

			double areaAccumulator = 0.0;
			double cxAccumulator = 0.0;
			double cyAccumulator = 0.0;

			int count = vertices.Count;

			for (int i = 0; i < count; i++)
			{
				PointByte2D current = vertices[i];
				PointByte2D next = vertices[(i + 1) % count];

				double xi = current.X;
				double yi = current.Y;
				double xj = next.X;
				double yj = next.Y;

				double cross = (xi * yj) - (xj * yi);

				areaAccumulator += cross;
				cxAccumulator += (xi + xj) * cross;
				cyAccumulator += (yi + yj) * cross;
			}

			double area = areaAccumulator * 0.5;

			if (Math.Abs(area) < double.Epsilon)
			{
				double sumX = 0.0;
				double sumY = 0.0;

				for (int i = 0; i < count; i++)
				{
					sumX += vertices[i].X;
					sumY += vertices[i].Y;
				}

				centerX = sumX / count;
				centerY = sumY / count;
				return;
			}

			double factor = 1.0 / (6.0 * area);

			centerX = cxAccumulator * factor;
			centerY = cyAccumulator * factor;
		}

		public void Cls()
		{
			lock (_lock)
			{
				ClearBitmap();
				ClearAttributes();
			}
		}
		public void ClearBitmap()
		{
			for (int c = 0; c < 6144; c++)
			{
				_host.screen[c] = 0;
			}
		}
		public void ClearAttributes()
		{
			for (int c = 6144; c < 6144 + 768; c++)
			{
				_host.screen[c] = 0;
			}
		}
		public void ClearCopper()
		{
			for (int c = 0; c < 192; c++)
			{
				_copper_intensity[c] = 0;
				_copper_palette[c] = 0;
			}
		}

		public void Reset()
		{
			VideoMode = 0;
			ClearCopper();
		}
		public void RegisterDevice(zx_spectrum hostmachine)
		{
			hostmachine.io_devices.Remove(this);
			hostmachine.io_devices.Add(this);
			_host = hostmachine;
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
		private byte[] userrombasedata = new byte[32 * 8]
		{
			255,128,128,128,128,128,128,128,
			255,128,191,160,160,160,160,160,
			255,128,191,160,175,168,168,168,
			255,128,191,160,175,168,171,170,
			0,127,64,64,64,64,64,64,
			0,127,64,95,80,80,80,80,
			0,127,64,95,80,87,84,84,
			0,127,64,95,80,87,84,85,
			64,128,0,0,0,0,0,0,
			72,144,32,64,128,0,0,0,
			73,146,36,72,144,32,64,128,
			73,146,36,73,146,36,72,144,
			73,146,36,73,146,36,73,146,
			201,210,36,89,154,36,75,147,
			217,218,36,91,155,36,75,147,
			217,218,36,219,219,36,91,155,
			219,219,36,219,219,36,219,219,
			219,129,0,129,129,0,129,219,
			219,129,60,165,165,60,129,219,
			255,165,255,165,165,255,165,255,
			240,0,0,0,0,0,0,15,
			240,0,120,0,0,30,0,15,
			241,1,255,129,129,255,128,143,
			253,37,37,37,164,164,164,191,
			136,34,136,34,136,34,136,34,
			85,0,170,0,85,0,170,0,
			136,170,170,170,170,170,170,34,
			127,0,254,0,127,0,254,0,
			129,66,36,24,24,24,24,255,
			129,130,132,248,248,132,130,129,
			255,24,24,24,24,36,66,129,
			129,65,33,31,31,33,65,129
		};
		/// <summary>
		/// Converts the bitmap portion of a ZX Spectrum .scr file to a 1bpp PNG.
		/// Ignores attribute bytes (colors/flash/bright).
		/// </summary>
		/// <param name="scrPath">Path to .scr file</param>
		/// <param name="pngPath">Destination PNG path</param>
		public static byte[] ConvertScrBitmapTo1BppPng(byte[] scrData)
		{
			// ZX Spectrum screen constants
			const int Width = 256;
			const int Height = 192;
			const int BitmapSize = 6144; // 256 * 192 / 8

			byte[] filebytes = null;
			if (scrData.Length < BitmapSize)
			{
				throw new ArgumentException("Invalid SCR file: bitmap data missing.");
			}

			// First 6144 bytes are the bitmap
			byte[] bitmapData = new byte[BitmapSize];
			Array.Copy(scrData, 0, bitmapData, 0, BitmapSize);

			using (Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format1bppIndexed))
			{
				BitmapData bmpData = bmp.LockBits(
				new Rectangle(0, 0, Width, Height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format1bppIndexed);
				try
				{
					int stride = bmpData.Stride;
					byte[] output = new byte[stride * Height];

					for (int y = 0; y < Height; y++)
					{
						// ZX Spectrum screen memory layout:
						// Y bits are rearranged as:
						// 010TTSSS LLLCCCCC
						int spectrumRow =
							((y & 0xC0) << 5) |  // bits 6-7
							((y & 0x07) << 8) |  // bits 0-2
							((y & 0x38) << 2);   // bits 3-5

						int srcOffset = spectrumRow;

						int dstOffset = y * stride;

						// 256 pixels / 8 = 32 bytes per row
						for (int xByte = 0; xByte < 32; xByte++)
						{
							output[dstOffset + xByte] =
								bitmapData[srcOffset + xByte];
						}
					}

					Marshal.Copy(output, 0, bmpData.Scan0, output.Length);
				}
				finally
				{
					bmp.UnlockBits(bmpData);
				}

				// Ensure palette is black/white
				ColorPalette palette = bmp.Palette;
				palette.Entries[0] = Color.White;
				palette.Entries[1] = Color.Black;
				bmp.Palette = palette;
				MemoryStream ms = new MemoryStream();
				bmp.Save(ms, ImageFormat.Png);
				filebytes = ms.ToArray();
			}
			return filebytes;
		}

		const int ScreenBase = 0x0000;
		const int AttrBase = 6144;
		public static readonly ushort[] rowTable = new ushort[192];
		static readonly ushort[] attrRowTable = new ushort[24];
		static readonly byte[] maskTable =
		{
			0x80,0x40,0x20,0x10,0x08,0x04,0x02,0x01
		};

		static void BuildRowTable()
		{
			for (int y = 0; y < 192; y++)
			{
				int offset =
					((y & 0xC0) << 5) |
					((y & 0x07) << 8) |
					((y & 0x38) << 2);

				rowTable[y] = (ushort)(ScreenBase + offset);
			}
		}

		static void BuildAttrTable()
		{
			for (int row = 0; row < 24; row++)
			{
				attrRowTable[row] = (ushort)(AttrBase + row * 32);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetPixel(byte[] mem, int x, int y)
		{
			int addr = rowTable[y] + (x >> 3);
			mem[addr] |= maskTable[x & 7];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetPixelAndAttr(byte[] mem, int x, int y, byte attr)
		{
			int addr = rowTable[y] + (x >> 3);
			mem[addr] |= maskTable[x & 7];
			int attrAddr = attrRowTable[y >> 3] + (x >> 3);
			mem[attrAddr] = attr;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool GetPixel(byte[] mem, int x, int y)
		{
			int addr = rowTable[y] + (x >> 3);
			return (mem[addr] & maskTable[x & 7]) != 0;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public (bool pixelState, byte attribute) GetPixelAndAttr(byte[] mem, int x, int y)
		{
			if (x < 0 || x >= 256 || y < 0 || y >= 192)
			{
				return (false, 0); // Return default for out-of-bounds
			}

			// Get pixel state
			int pixelAddr = rowTable[y] + (x >> 3);
			bool pixelState = (mem[pixelAddr] & maskTable[x & 7]) != 0;

			// Get attribute
			int attrAddr = attrRowTable[y >> 3] + (x >> 3);
			byte attribute = mem[attrAddr];

			return (pixelState, attribute);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearPixel(byte[] mem, int x, int y)
		{
			int addr = rowTable[y] + (x >> 3);
			mem[addr] &= (byte)~maskTable[x & 7];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetAttribute(byte[] mem, int x, int y, byte attr)
		{
			int attrAddr = attrRowTable[y >> 3] + (x >> 3);
			mem[attrAddr] = attr;
		}
	}
}

