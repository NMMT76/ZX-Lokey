using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LOKEY
{
	[Flags]
	public enum Bit16
	{
		Bit0 = 0x001,
		Bit1 = 1 << 1,
		Bit2 = 1 << 2,
		Bit3 = 1 << 3,
		Bit4 = 1 << 4,
		Bit5 = 1 << 5,
		Bit6 = 1 << 6,
		Bit7 = 1 << 7,
		Bit8 = 1 << 8,
		Bit9 = 1 << 9,
		Bit10 = 1 << 10,
		Bit11 = 1 << 11,
		Bit12 = 1 << 12,
		Bit13 = 1 << 13,
		Bit14 = 1 << 14,
		Bit15 = 1 << 15
	};
	public static class LOKEY_Utility_Types
	{
		private static byte[] IntToBytes(int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			return new byte[] { bytes[2], bytes[1], bytes[0] };
		}
		private static int BytesToInt(byte[] bytes)
		{
			return bytes[0] * 256 * 256 + bytes[1] * 256 + bytes[2];
		}
		private static byte[] ReadBytesFromOutQueue(Queue<byte> outqueue, int numbytes)
		{
			byte[] bytes = new byte[numbytes];
			for (int c = 0; c < numbytes; c++)
			{
				bytes[c] = outqueue.Dequeue();
			}
			return bytes;
		}

		public static void WriteULongToInQueue(Queue<byte> inqueue, UInt32 value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			WriteBytesToInQueue(inqueue,bytes);
		}
		public static ushort ReadUIntegerFromOutQueue(Queue<byte> outqueue)
		{
			byte[] bytes = ReadBytesFromOutQueue(outqueue,2);
			return (ushort)(bytes[0] + bytes[1] * 256);
		}
		public static void WriteUIntegerToInQueue(Queue<byte> inqueue,UInt16 value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			WriteBytesToInQueue(inqueue, bytes);
		}
		public static void WriteBytesToInQueue(Queue<byte> inqueue,byte[] bytevalues)
		{
			for (int c = 0; c < bytevalues.Length; c++)
			{
				inqueue.Enqueue(bytevalues[c]);
			}
		}
		public static double ReadZXFloatFromOutQueue(Queue<byte> outqueue)
		{
			double retval = 0.0;
			byte[] bytes = ReadBytesFromOutQueue(outqueue,5);
			retval = ZXUtility.ZXFloatToDouble(bytes);
			return retval;
		}
		public static ushort ReadUIntegerFromMemory(IList<byte> hostram, ushort addr)
		{
			ushort retval = 0;
			byte[] bytes = new byte[2];
			for (int c = 0; c < 2; c++)
			{
				bytes[c] = hostram[(ushort)(addr + c)];
			}
			retval = BitConverter.ToUInt16(bytes, 0);
			return retval;
		}
		public static ulong ReadULongFromMemory(IList<byte> hostram, ushort addr)
		{
			ulong retval = 0;
			byte[] bytes = new byte[4];
			for (int c = 0; c < 4; c++)
			{
				bytes[c] = hostram[(ushort)(addr + c)];
			}
			retval = BitConverter.ToUInt32(bytes, 0);
			return retval;
		}
		public static string ReadStringFromMemory(IList<byte> hostram, ushort addr)
		{
			string retval = string.Empty;
			List<byte> tempbytes = new List<byte>();
			while (hostram[(addr)] != 0)
			{
				tempbytes.Add(hostram[(addr)]);
				addr++;
			}
			retval = Encoding.ASCII.GetString(tempbytes.ToArray());
			return retval;
		}
		public static double ReadZXFloatFromMemory(IList<byte> hostram, ushort addr)
		{
			double retval = 0.0;
			byte[] bytes = new byte[5];
			for (int c = 0; c < 5; c++)
			{
				bytes[c] = hostram[(ushort)(addr + c)];
			}
			retval = ZXUtility.ZXFloatToDouble(bytes);
			return retval;
		}
		public static double ReadZXFixedFromMemory(ZxRam hostram, ushort addr)
		{
			double retval = 0.0;
			byte[] bytes = new byte[4];
			for (int c = 0; c < 4; c++)
			{
				bytes[c] = hostram[(ushort)(addr + c)];
			}
			retval = FixedToDouble(bytes);
			return retval;
		}
		public	static Vector3 ReadVector3FromMemory(ZxRam hostram, ushort addr)
		{
			double x = ReadZXFloatFromMemory(hostram, addr);
			double y = ReadZXFloatFromMemory(hostram, (ushort)(addr + 5));
			double z = ReadZXFloatFromMemory(hostram, (ushort)(addr + 10));
			return new Vector3((float)x, (float)y, (float)z);
		}
		public static void WriteZXFloatToInQueue(Queue<byte> inqueue,double value)
		{
			byte[] bytes = ZXUtility.DoubleToZXFloat(Math.Round(value, 8));
			WriteBytesToInQueue(inqueue,bytes);
		}
		public static void WriteZXFloatToMemory(ZxRam hostram, ushort addr, double val)
		{
			byte[] bytes = ZXUtility.DoubleToZXFloat(Math.Round(val, 8));
			for (int c = 0; c < 5; c++)
			{
				hostram[addr + c] = bytes[c];
			}
		}
		public static void WriteZXFixedToMemory(ZxRam hostram, ushort addr, double val)
		{
			byte[] bytes = DoubleToFixed(val);
			for (int c = 0; c < 4; c++)
			{
				hostram[addr + c] = bytes[c];
			}
		}
		public static void WriteStringToMemory(IList<byte> hostram, ushort addr, string outstr)
		{
			for (int c = 0; c < outstr.Length; c++)
			{
				hostram[addr + c] = (byte)outstr[c];
			}
		}
		public static double FixedToDouble(byte[] fixedPointBytes)
		{
			if (fixedPointBytes == null || fixedPointBytes.Length != 4)
			{
				throw new ArgumentException("Fixed point value must be represented by exactly 4 bytes.", nameof(fixedPointBytes));
			}

			// Boriel Basic Fixed point is 32-bit: 1 bit sign, 15 bits integer, 16 bits fractional.
			// The value is stored in little-endian format.

			// Reconstruct the 32-bit signed integer value
			int signedValue = fixedPointBytes[0] | (fixedPointBytes[1] << 8) | (fixedPointBytes[2] << 16) | (fixedPointBytes[3] << 24);

			// The fractional part has 16 bits, so the divisor is 2^16
			return (double)signedValue / 65536.0;
		}
		public static byte[] DoubleToFixed(double value)
		{
			// Boriel Basic Fixed point is 32-bit: 1 bit sign, 15 bits integer, 16 bits fractional.
			// The maximum positive value for Boriel Basic Fixed point is approximately 32767.99998.
			// The minimum negative value for Boriel Basic Fixed point is approximately -32768.0.

			if (value > 32767.99998474121 || value < -32768.0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Value is outside the representable range for Boriel Basic Fixed point (-32768.0 to 32767.99998...).");
			}

			// Multiply by 2^16 to shift the fractional part into the integer part.
			// Then cast to int to truncate.
			int signedValue = (int)Math.Round(value * 65536.0);

			// Convert the 32-bit signed integer to a 4-byte little-endian array.
			byte[] fixedPointBytes = new byte[4];
			fixedPointBytes[0] = (byte)signedValue;
			fixedPointBytes[1] = (byte)(signedValue >> 8);
			fixedPointBytes[2] = (byte)(signedValue >> 16);
			fixedPointBytes[3] = (byte)(signedValue >> 24);

			return fixedPointBytes;
		}
		public static int RemapGeneric(float value, float oldMax, float newMax)
		{
			// 1. Normalize value to 0.0 - 1.0
			float normalized = value / oldMax;

			// 2. Scale to new range
			float result = normalized * newMax;

			// 3. Round to nearest integer
			return (int)Math.Round(result);
		}
	}
	public static class ZXUtility
	{
		//ZX memory constants
		public const int MemoryScreenStart = 16384;
		public const int MemoryScreenEnd = 22528;
		public const int MemoryScreenLength = 6144;
		//ZX "utility constants"
		public const int ZXResolutionWidth = 256;
		public const int ZXResolutionHeight = 192;
		//ZX "safe" Tstate to begin doing Quasi-DMA
		public const int ZXMaxTStateForDMA = 10000;
		//ZX memory "line offsets", allow calculating any 8x1 "pixel block positions" by simply
		//doing MemoryScreenStart+PreCalculatedScreenMemoryOffsets[y]+(xoffset/8)
		//Mind you that drawing to the screen using direct memory access would require you to do
		//both a read and a write to that block if you wanted to change a single pixel.
		//Highly inneficient without coprocessors helping, and one of the reasons bitplanes totally
		//fell out of "grace" once you needed 256 or more colors as changing 8 bitplanes was much
		//more expensive than writing a single byte value
		public static readonly int[] PreCalculatedScreenMemoryOffsets;
		//"utility constants"
		public const float RadPerDegree = (float)(Math.PI / 180.0);
		static ZXUtility()
		{
			PreCalculatedScreenMemoryOffsets = new int[192]
			{
					0000, 0256, 0512, 0768, 1024, 1280, 1536, 1792, 0032, 0288, 0544, 0800, 1056, 1312, 1568, 1824,
					0064, 0320, 0576, 0832, 1088, 1344, 1600, 1856, 0096, 0352, 0608, 0864, 1120, 1376, 1632, 1888,
					0128, 0384, 0640, 0896, 1152, 1408, 1664, 1920, 0160, 0416, 0672, 0928, 1184, 1440, 1696, 1952,
					0192, 0448, 0704, 0960, 1216, 1472, 1728, 1984, 0224, 0480, 0736, 0992, 1248, 1504, 1760, 2016,
					2048, 2304, 2560, 2816, 3072, 3328, 3584, 3840, 2080, 2336, 2592, 2848, 3104, 3360, 3616, 3872,
					2112, 2368, 2624, 2880, 3136, 3392, 3648, 3904, 2144, 2400, 2656, 2912, 3168, 3424, 3680, 3936,
					2176, 2432, 2688, 2944, 3200, 3456, 3712, 3968, 2208, 2464, 2720, 2976, 3232, 3488, 3744, 4000,
					2240, 2496, 2752, 3008, 3264, 3520, 3776, 4032, 2272, 2528, 2784, 3040, 3296, 3552, 3808, 4064,
					4096, 4352, 4608, 4864, 5120, 5376, 5632, 5888, 4128, 4384, 4640, 4896, 5152, 5408, 5664, 5920,
					4160, 4416, 4672, 4928, 5184, 5440, 5696, 5952, 4192, 4448, 4704, 4960, 5216, 5472, 5728, 5984,
					4224, 4480, 4736, 4992, 5248, 5504, 5760, 6016, 4256, 4512, 4768, 5024, 5280, 5536, 5792, 6048,
					4288, 4544, 4800, 5056, 5312, 5568, 5824, 6080, 4320, 4576, 4832, 5088, 5344, 5600, 5856, 6112
			};
		}
		public static int GetScreenMemoryLineOffsetFromBase(int y)
		{
			/*
			Byte 0	Byte 1
			15	14	13	12	11	10	9	8	7	6	5	4	3	2	1	0
			0	1	0	Y7	Y6	Y2	Y1	Y0	Y5	Y4	Y3	0	0	0	0	0
			*/
			int offset = 0b1111111111111111;

			int mask210 = (int)(Bit16.Bit2 | Bit16.Bit1 | Bit16.Bit0);
			int mask543 = (int)(Bit16.Bit5 | Bit16.Bit4 | Bit16.Bit3);
			int mask76 = (int)(Bit16.Bit7 | Bit16.Bit6);

			int bits210 = (y & mask210) << 8;
			int bits543 = (y & mask543) << 2;
			int bits76 = (y & mask76) << 5;

			offset &= (bits210 | bits543 | bits76 | (int)Bit16.Bit14);
			offset -= 16384;

			return offset;
		}
		public static bool IsBitSet(byte b, int pos)
		{
			return (b & (1 << pos)) != 0;
		}
		public static void DrawLineIndexedColor(byte x, byte y, byte x2, byte y2, byte color, byte[,] array)
		{
			int w = x2 - x;
			int h = y2 - y;
			int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
			if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
			if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
			if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
			int longest = Math.Abs(w);
			int shortest = Math.Abs(h);
			if (longest <= shortest)
			{
				longest = Math.Abs(h);
				shortest = Math.Abs(w);
				if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
				dx2 = 0;
			}
			int numerator = longest >> 1;
			for (int i = 0; i <= longest; i++)
			{
				array[x, y] = color;
				numerator += shortest;
				if (numerator >= longest)
				{
					numerator -= longest;
					x = (byte)(x + dx1);
					y = (byte)(y + dy1);
				}
				else
				{
					x = (byte)(x + dx2);
					y = (byte)(y + dy2);
				}
			}
		}
		public static void ListAdd(List<byte> bytelist, int x, int y)
		{
			bytelist.Add((byte)(x));
			bytelist.Add((byte)(y));
		}
		public static byte[] MidpointCircleAlgorithm(int screenwidth, int screenheight, int centerx, int centery, int radius)
		{
			List<byte> pointlist = new List<byte>();
			List<Tuple<int, int>> mpcapoints = new List<Tuple<int, int>>();
			int x = 0;
			int y = radius;
			float decisionParameter = 1 - radius;

			PlotMPCA(x, y, centerx, centery); // Plot the initial point

			while (x < y)
			{
				x++;

				if (decisionParameter < 0)
				{
					decisionParameter += 2 * x + 1;
				}
				else
				{
					y--;
					decisionParameter += 2 * (x - y) + 1;
				}

				PlotMPCA(x, y, centerx, centery);
			}
			foreach (Tuple<int, int> kvp in mpcapoints)
			{
				if (kvp.Item1 >= 0 && kvp.Item1 <= screenwidth - 1 && kvp.Item2 >= 0 && kvp.Item2 <= screenheight - 1)
				{
					pointlist.Add((byte)kvp.Item1);
					pointlist.Add((byte)kvp.Item2);
				}
			}
			return pointlist.ToArray();
			void PlotMPCA(int xt, int yt, int cxt, int cyt)
			{
				// Example using a list to store coordinates.  Replace with your drawing logic
				mpcapoints.Add(new Tuple<int, int>(cxt + xt, cyt + yt));
				mpcapoints.Add(new Tuple<int, int>(cxt - xt, cyt + yt));
				mpcapoints.Add(new Tuple<int, int>(cxt + xt, cyt - yt));
				mpcapoints.Add(new Tuple<int, int>(cxt - xt, cyt - yt));
				mpcapoints.Add(new Tuple<int, int>(cxt + yt, cyt + xt));
				mpcapoints.Add(new Tuple<int, int>(cxt - yt, cyt + xt));
				mpcapoints.Add(new Tuple<int, int>(cxt + yt, cyt - xt));
				mpcapoints.Add(new Tuple<int, int>(cxt - yt, cyt - xt));
			}
		}

		public static byte[] BitmapToByteArray(Bitmap bitmap)
		{
			BitmapData bmpdata = null;
			try
			{
				bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				int numbytes = (bmpdata.Stride * bitmap.Height);
				byte[] bytedata = new byte[numbytes];
				IntPtr ptr = bmpdata.Scan0;
				Marshal.Copy(ptr, bytedata, 0, numbytes);
				return bytedata;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			finally
			{
				if (bmpdata != null) { bitmap.UnlockBits(bmpdata); }
			}
			return null;
		}

		public static byte[] StringToByteArray(string str, Encoding enc)
		{
			if (enc == Encoding.ASCII)
			{
				return Encoding.ASCII.GetBytes(str);
			}
			if (enc == Encoding.UTF8)
			{
				return Encoding.UTF8.GetBytes(str);
			}
			return null;
		}
		public static byte[] ByteArrayToScreenMemoryArray(byte[] input)
		{
			byte[] retval = new byte[input.Length];
			int coffset = 0;
			for (int y = 0; y < 192; y++)
			{
				for (int c = 0; c < 32; c++)
				{
					retval[ZXUtility.PreCalculatedScreenMemoryOffsets[y] + c] = input[coffset + c];
				}
				coffset += 32;
			}
			return retval;
		}
		public static List<byte> ByteArrayToQuarterScreenMemoryArray(byte[] input)
		{
			//Quarter resolution with 2 extra bytes per line, which will be the memory offset for the next 16bytes
			List<byte> retval = new List<byte>();
			for (int y = 48; y < 48 + 96; y++)
			{
				//Add the memory offset, which is row offset plus 12bytes
				UInt16 rowoffset = (UInt16)(ZXUtility.PreCalculatedScreenMemoryOffsets[y] + 8);
				byte lowbyte = (byte)(rowoffset & 0x00FF);
				byte highbyte = (byte)((rowoffset & 0xFF00) >> 8);
				retval.Add(lowbyte);
				retval.Add(highbyte);
				for (int c = 8; c < 24; c++)
				{
					retval.Add(input[c - 8]);
				}
			}
			return retval;
		}
		public static byte[] CompressGZIP(byte[] bytes)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
				{
					gzipStream.Write(bytes, 0, bytes.Length);
				}
				return memoryStream.ToArray();
			}
		}
		public static byte[] DecompressGZIP(byte[] bytes)
		{
			using (var memoryStream = new MemoryStream(bytes))
			{
				using (var outputStream = new MemoryStream())
				{
					using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
					{
						decompressStream.CopyTo(outputStream);
					}
					return outputStream.ToArray();
				}
			}
		}
		public static float BBFixedToFloat(byte[] bytes)
		{
			float retval;
			Int16 ip;
			float fp;
			//Fractional part
			fp = (float)((bytes[0] + bytes[1] * 256) / 65536.0f);
			//Check sign
			if (bytes[3] < 128)
			{
				ip = (Int16)(bytes[2] + bytes[3] * 256);
			}
			else
			{
				ip = (Int16)(BitConverter.ToInt16(bytes, 2));
			}
			if (bytes[0] == 0 && bytes[1] == 0)
			{
				retval = ip;
			}
			else
			{
				retval = fp + ip;
			}
			return retval;
		}
		public static byte[] FloatToBBFixed(float dval)
		{
			byte[] retavl;
			//get ip and fp parts
			Int16 ip = (Int16)dval;
			float tempfp = (float)(dval - ip);
			UInt16 fp;
			fp = (UInt16)((dval - ip) * 65536.0f);
			if (dval < 0 && fp != 0)
			{
				ip = (Int16)(ip - 1);
			}
			//get bytes
			byte[] ipar = BitConverter.GetBytes(ip);
			byte[] fpar = BitConverter.GetBytes(fp);
			retavl = new byte[4] { fpar[0], fpar[1], ipar[0], ipar[1] };
			return retavl;
		}
		// Decode ZX 5-byte (or small-int) -> exact double
		public static double ZXFloatToDouble(byte[] zx)
		{
			if (zx == null || zx.Length != 5) throw new ArgumentException("ZX float must be exactly 5 bytes", nameof(zx));

			// small-int special case (first byte == 0)
			if (zx[0] == 0x00)
			{
				if (zx[1] == 0x00) // positive small int
					return (double)(zx[2] | (zx[3] << 8));
				if (zx[1] == 0xFF) // negative small int (two's complement stored)
				{
					int stored = zx[2] | (zx[3] << 8);
					return (double)(stored - 65536);
				}
				// defensive fallback
				return (double)(zx[2] | (zx[3] << 8));
			}

			bool negative = (zx[1] & 0x80) != 0;
			uint b1 = (uint)(zx[1] & 0x7F);
			uint b2 = zx[2];
			uint b3 = zx[3];
			uint b4 = zx[4];

			// N = b1<<24 | b2<<16 | b3<<8 | b4  (0 .. 2^31-1)
			uint N = (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;

			// M_num = 2^31 + N
			uint M_num = (1u << 31) + N;

			// value = M_num * 2^(zxExp - 160)
			int zxExp = zx[0];
			double value = (double)M_num * Math.Pow(2.0, zxExp - 160);

			return negative ? -value : value;
		}

		// Encode double -> ZX 5 bytes. If preferSmallInt=true, exact integer in -65536..65535 will use small-int encoding.
		public static byte[] DoubleToZXFloat(double value, bool preferSmallInt = false)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				throw new ArgumentOutOfRangeException(nameof(value), "ZX format can't represent NaN/Infinity.");

			if (value == 0.0)
				return new byte[5]; // {0,0,0,0,0}

			// small-int encoding optional
			if (preferSmallInt)
			{
				double rounded = Math.Round(value);
				if (Math.Abs(value - rounded) < 1e-12) // exact integer within rounding tolerance
				{
					long n = (long)rounded;
					if (n >= -65536 && n <= 65535)
					{
						byte[] outBytes = new byte[5];
						outBytes[0] = 0x00;
						outBytes[1] = (byte)(n < 0 ? 0xFF : 0x00);
						ushort stored = (ushort)n; // two's complement for negative
						outBytes[2] = (byte)(stored & 0xFF);
						outBytes[3] = (byte)((stored >> 8) & 0xFF);
						outBytes[4] = 0x00;
						return outBytes;
					}
				}
			}

			bool negative = value < 0.0;
			double absVal = negative ? -value : value;

			// compute exponent so mantissa in [0.5, 1.0)
			int eUnbiased = (int)Math.Floor(Math.Log(absVal, 2.0)); // floor(log2(absVal))
			int zxExp = eUnbiased + 1 + 128;

			// mantissa = absVal / 2^(zxExp - 128)  -> should be in [0.5, 1.0)
			double mantissa = absVal / Math.Pow(2.0, zxExp - 128);

			// M_num = round(mantissa * 2^32)  (should be in [2^31 .. 2^32-1])
			double MnumD = Math.Round(mantissa * Math.Pow(2.0, 32));
			// handle rounding overflow (mantissa rounded to 1.0)
			if (MnumD >= Math.Pow(2.0, 32))
			{
				// increment exponent and use M_num = 2^31 (i.e. fractional part 0)
				zxExp++;
				MnumD = (double)(1u << 31);
			}
			if (MnumD < (double)(1u << 31)) MnumD = (double)(1u << 31); // guard

			uint Mnum = (uint)MnumD;
			uint N = Mnum - (1u << 31); // 31-bit stored mantissa

			byte[] res = new byte[5];
			res[0] = (byte)(zxExp & 0xFF);
			res[1] = (byte)(((N >> 24) & 0x7Fu) | (negative ? 0x80u : 0u));
			res[2] = (byte)((N >> 16) & 0xFFu);
			res[3] = (byte)((N >> 8) & 0xFFu);
			res[4] = (byte)(N & 0xFFu);
			return res;
		}
		public static string SanitizeFilename(string filename)
		{
			string retval = string.Empty;
			if (string.IsNullOrWhiteSpace(filename)) { return retval; }
			retval = filename;
			//No colons
			retval = retval.Replace(":", "");
			//No \\
			while (retval.Contains("\\\\"))
			{
				retval = retval.Replace("\\\\", "");
			}
			//No //
			while (retval.Contains("//"))
			{
				retval = retval.Replace("//", "");
			}
			//No ..
			while (retval.Contains(".."))
			{
				retval = retval.Replace("..", "");
			}
			//No ?
			retval = retval.Replace("?", "");
			//If first char is / or \, strip it
			if (retval[0] == '\\' || retval[0] == '/')
			{
				retval = retval.Substring(1, retval.Length - 1);
			}
			//No non ASCII 'human chars', restrictive yes, but safe
			for (int c = 0; c < retval.Length; c++)
			{
				if (retval[c] < 32 || retval[c] > 126)
				{
					retval = retval.Replace(retval[c], ' ');
				}
			}
			return retval;
		}
		//public static byte[] BitmapToScreenMemory(Bitmap source, int quantizer)
		//{
		//	Bitmap scaled = source.Resize(new Size(ZXUtility.ZXResolutionWidth, ZXUtility.ZXResolutionHeight), ScalingMode.Auto, false);
		//	Bitmap quantized = null;
		//	switch (quantizer)
		//	{
		//		case 0:
		//			quantized = scaled.ConvertPixelFormat(PixelFormat.Format1bppIndexed, PredefinedColorsQuantizer.BlackAndWhite());
		//			break;
		//		case 1:
		//			quantized = scaled.ConvertPixelFormat(PixelFormat.Format1bppIndexed, PredefinedColorsQuantizer.BlackAndWhite(), OrderedDitherer.Bayer2x2);
		//			break;
		//		case 2:
		//			quantized = scaled.ConvertPixelFormat(PixelFormat.Format1bppIndexed, PredefinedColorsQuantizer.BlackAndWhite(), OrderedDitherer.Bayer4x4);
		//			break;
		//		case 3:
		//			quantized = scaled.ConvertPixelFormat(PixelFormat.Format1bppIndexed, PredefinedColorsQuantizer.BlackAndWhite(), OrderedDitherer.Bayer8x8);
		//			break;
		//		case 4:
		//			quantized = scaled.ConvertPixelFormat(PixelFormat.Format1bppIndexed, PredefinedColorsQuantizer.BlackAndWhite(), ErrorDiffusionDitherer.FloydSteinberg);
		//			break;
		//		case 5:
		//			quantized = scaled.ConvertPixelFormat(PixelFormat.Format1bppIndexed, PredefinedColorsQuantizer.BlackAndWhite(), OrderedDitherer.DottedHalftone);
		//			break;
		//	}
		//	Bitmap reduced = quantized.Clone(new System.Drawing.Rectangle(0, 0, ZXUtility.ZXResolutionWidth, ZXUtility.ZXResolutionHeight), PixelFormat.Format1bppIndexed);
		//	byte[] bytes = ZXUtility.BitmapToByteArray(reduced);
		//	reduced.Dispose();
		//	quantized.Dispose();
		//	scaled.Dispose();
		//	source.Dispose();
		//	byte[] screenmemoryarray = ZXUtility.ByteArrayToScreenMemoryArray(bytes);
		//	return screenmemoryarray;
		//}
		// ZX Spectrum screen memory starts at 16384 (0x4000)
		private const uint SCREEN_BASE = 0x4000;

		/// <summary>
		/// Calculates the ZX Spectrum screen memory address and pixel bitmask
		/// for a given X,Y coordinate
		/// </summary>
		/// <param name="x">X coordinate (0-255)</param>
		/// <param name="y">Y coordinate (0-191)</param>
		/// <param name="address">Output: Memory address for this pixel's byte</param>
		/// <param name="bitmask">Output: Bitmask for the specific pixel within the byte</param>
		public static void GetPixelAddressAndMask(byte x, byte y, out uint address, out byte bitmask)
		{
			// The ZX Spectrum screen layout is complex due to its unusual memory arrangement
			// Screen resolution: 256x192 pixels
			// Each byte contains 8 pixels (1 bit per pixel)

			// Calculate which byte column (0-31, since 256/8 = 32)
			byte byteColumn = (byte)(x >> 3);  // x / 8

			// Calculate pixel bit position within the byte (0-7)
			byte bitPosition = (byte)(7 - (x & 7));  // 7 - (x % 8)

			// Create bitmask for this pixel
			bitmask = (byte)(1 << bitPosition);

			// ZX Spectrum screen memory layout:
			// The screen is divided into thirds (0-63, 64-127, 128-191)
			// Within each third, lines are interleaved in a complex pattern

			// Break Y into components for the ZX Spectrum's memory layout:
			// Bits 7-6: Which third of screen (0-2)
			// Bits 5-3: Character row within third (0-7)
			// Bits 2-0: Scan line within character (0-7)

			byte yThird = (byte)((y & 0b11000000) >> 6);    // Bits 7-6
			byte yChar = (byte)((y & 0b00111000) >> 3);      // Bits 5-3
			byte yScan = (byte)(y & 0b00000111);             // Bits 2-0

			// Calculate address using ZX Spectrum's screen layout formula:
			// Address = 16384 + (yThird * 2048) + (yScan * 256) + (yChar * 32) + byteColumn
			uint offset = (uint)((yThird * 2048) + (yScan * 256) + (yChar * 32) + byteColumn);
			address = SCREEN_BASE + offset;
		}
		public enum ZXColor
		{
			Black,
			Blue,
			Red,
			Magenta,
			Green,
			Cyan,
			Yellow,
			White,
			BrightBlack,
			BrightBlue,
			BrightRed,
			BrightMagenta,
			BrightGreen,
			BrightCyan,
			BrightYellow,
			BrightWhite
		}

		// 4-bit ULA colour patterns: BRIGHT BRG
		public static readonly byte[] ZXColorBitPatterns =
		{
					0b0000, // Black
					0b0100, // Blue
					0b0010, // Red
					0b0110, // Magenta
					0b0001, // Green
					0b0101, // Cyan
					0b0011, // Yellow
					0b0111, // White
					0b1000, // Bright Black
					0b1100, // Bright Blue
					0b1010, // Bright Red
					0b1110, // Bright Magenta
					0b1001, // Bright Green
					0b1101, // Bright Cyan
					0b1011, // Bright Yellow
					0b1111  // Bright White
			};

		// Build a ZX Spectrum attribute byte
		// ink:  ZxColor used for INK  (bits 0–2)
		// paper: ZxColor used for PAPER (bits 3–5)
		public static byte BuildAttribute(ZXColor ink, ZXColor paper)
		{
			byte inkPattern = ZXColorBitPatterns[(int)ink];
			byte paperPattern = ZXColorBitPatterns[(int)paper];

			byte inkValue = (byte)(inkPattern & 0b111);            // bits 0–2
			byte paperValue = (byte)((paperPattern & 0b111) << 3);     // bits 3–5

			byte inkBright = (byte)((inkPattern >> 3) & 1);
			byte paperBright = (byte)((paperPattern >> 3) & 1);

			byte bright = (byte)((inkBright | paperBright) << 6);      // shared BRIGHT bit

			// FLASH (bit 7) omitted (always 0)
			return (byte)(inkValue | paperValue | bright);
		}
	}
}

