using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speccy.Devices.ULAZX
{
	public class PointByte2D
	{
		public byte X { get; set; }
		public byte Y { get; set; }

		public PointByte2D(byte x, byte y)
		{
			X = x;
			Y = y;
		}
	}
	public static class ZxScreenAddress
	{
		private const int ScreenStart = 0x4000;
		private const int ScreenSize = 6144;

		private static readonly PointByte2D[] AddressToPoint = CreateTable();

		private static PointByte2D[] CreateTable()
		{
			var table = new PointByte2D[ScreenSize];

			for (int y = 0; y < 192; y++)
			{
				int rowBase =
					((y & 0xC0) << 5) |
					((y & 0x07) << 8) |
					((y & 0x38) << 2);

				for (int xb = 0; xb < 32; xb++)
				{
					int addr = rowBase + xb;

					byte x = (byte)(xb * 8);

					table[addr] = new PointByte2D(x, (byte)y);
				}
			}

			return table;
		}

		public static PointByte2D AddressToPixel(int address)
		{
			int offset = address - ScreenStart;
			if ((uint)offset >= ScreenSize) return null;
			return AddressToPoint[offset];
		}
	}
}
