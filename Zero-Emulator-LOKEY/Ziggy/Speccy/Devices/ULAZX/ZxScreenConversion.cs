using System;
using System.Collections;
using System.Collections.Generic;

public static class ZxScreenConversion
{
	private static readonly int[] SrcPixelIndex = CreatePixelIndexTable();

	private static int[] CreatePixelIndexTable()
	{
		const int width = 256;
		const int height = 192;

		int[] table = new int[6144];
		int zxIndex = 0;

		for (int y = 0; y < height; y++)
		{
			int rowBase =
				((y & 0xC0) << 5) |
				((y & 0x07) << 8) |
				((y & 0x38) << 2);

			for (int xb = 0; xb < 32; xb++)
			{
				int zxAddr = rowBase + xb;
				table[zxAddr] = y * width + xb * 8;
			}
		}
		return table;
	}

	public static byte[] LinearPixelmapToZxBitmap(ReadOnlySpan<byte> linear)
	{
		if (linear.Length != 49152)
		{
			throw new ArgumentException("Input must be 256×192 pixels.");
		}
		byte[] zx = new byte[6144];
		for (int i = 0; i < 6144; i++)
		{
			int src = SrcPixelIndex[i];

			byte value =
				(byte)(
				(linear[src] << 7) | (linear[src + 1] << 6) | (linear[src + 2] << 5) | (linear[src + 3] << 4) |
				(linear[src + 4] << 3) | (linear[src + 5] << 2) | (linear[src + 6] << 1) | (linear[src + 7]));

			zx[i] = value;
		}
		return zx;
	}
	public static void ConvertZXBitmapToLinearPixelmap(IList<byte> spectrumBitmap,IList<byte> destination)
	{
		for (int y = 0; y < 192; y++)
		{
			int srcRowOffset =((y & 0xC0) << 5) |((y & 0x07) << 8) |((y & 0x38) << 2);

			int dstRowOffset = y * 256;

			for (int xByte = 0; xByte < 32; xByte++)
			{
				byte b = spectrumBitmap[srcRowOffset + xByte];
				int x = dstRowOffset + (xByte << 3);

				destination[x + 0] = (byte)((b >> 7) & 1);
				destination[x + 1] = (byte)((b >> 6) & 1);
				destination[x + 2] = (byte)((b >> 5) & 1);
				destination[x + 3] = (byte)((b >> 4) & 1);
				destination[x + 4] = (byte)((b >> 3) & 1);
				destination[x + 5] = (byte)((b >> 2) & 1);
				destination[x + 6] = (byte)((b >> 1) & 1);
				destination[x + 7] = (byte)(b & 1);
			}
		}
	}
}