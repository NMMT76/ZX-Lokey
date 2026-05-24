//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lokey
//{
//	public static class ZXVideoMemory
//	{
//		public static int[] _yLineOffsets { get; private set; } // Static member to hold the generated offsets
//		private const int SCREEN_MEMORY_START_OFFSET = 0x4000; // Represents $4000 as index 0 in zxRam
//		private const int SCREEN_MEMORY_SIZE = 6912; // $5AFF - $4000 + 1
//		private const int SCREEN_WIDTH_BYTES = 32; // 256 pixels / 8 bits per byte

//		// Static constructor to ensure the Y-line offsets table is available and initialized once.
//		static ZXVideoMemory()
//		{
//			_yLineOffsets = CreateYLineOffsetTable();
//		}

//		/// <summary>
//		/// Creates an array of 192 integers, where each integer represents the memory offset
//		/// for a given Y-line relative to the start of the ZX Spectrum's display memory ($4000).
//		/// This method can be called externally, or used internally to populate the static _yLineOffsets member.
//		/// </summary>
//		/// <returns>An int[] array containing the memory offsets for each Y-line.</returns>
//		public static int[] CreateYLineOffsetTable()
//		{
//			int[] yLineOffsets = new int[192];

//			for (int y = 0; y < 192; y++)
//			{
//				int blockOffset = (y & 0xC0) << 5;
//				int lineInBlockOffset = (y & 0x07) << 8;
//				int charBlockRowOffset = (y & 0x38) >> 3;

//				yLineOffsets[y] = blockOffset | lineInBlockOffset | charBlockRowOffset;
//			}

//			return yLineOffsets;
//		}

//		/// <summary>
//		/// Plots a pixel in the ZX Spectrum video memory.
//		/// </summary>
//		/// <param name="zxRam">The byte array representing the ZX Spectrum RAM, where index 0 is $4000.</param>
//		/// <param name="yLineOffsets">The pre-calculated array of Y-line memory offsets.</param>
//		/// <param name="x">The X-coordinate of the pixel (0-255).</param>
//		/// <param name="y">The Y-coordinate of the pixel (0-191).</param>
//		/// <param name="setPixel">True to set the pixel (foreground color), false to reset (background color).</param>
//		public static void Plot(IList<byte> zxRam, int[] yLineOffsets, byte x, byte y, bool setPixel)
//		{
//			if (zxRam == null)
//			{
//				throw new ArgumentNullException(nameof(zxRam), "ZX Spectrum RAM array cannot be null.");
//			}

//			if (yLineOffsets == null)
//			{
//				throw new ArgumentNullException(nameof(yLineOffsets), "Y-line offsets table cannot be null.");
//			}

//			// The yLineOffsets array must contain 192 elements (0-191).
//			if (yLineOffsets.Length != 192)
//			{
//				throw new ArgumentException("The Y-line offsets table must contain exactly 192 elements.", nameof(yLineOffsets));
//			}

//			if (y > 191)
//			{
//				throw new ArgumentOutOfRangeException(nameof(y), "Y coordinate must be between 0 and 191.");
//			}

//			// The yLineOffsets argument is explicitly used here.
//			int yLineBaseOffset = yLineOffsets[y];

//			int byteAddress = SCREEN_MEMORY_START_OFFSET + yLineBaseOffset + (x >> 3);

//			byte bitMask = (byte)(0x80 >> (x & 0x07));

//			//if (byteAddress >= zxRam.Count || byteAddress < 0 || byteAddress >= SCREEN_MEMORY_SIZE)
//			//{
//			//	throw new IndexOutOfRangeException($"Calculated relative memory address 0x{byteAddress:X4} (absolute 0x{0x4000 + byteAddress:X4}) is out of bounds for the provided ZX RAM array or screen memory area.");
//			//}

//			if (setPixel)
//			{
//				zxRam[byteAddress] |= bitMask;
//			}
//			else
//			{
//				zxRam[byteAddress] &= (byte)~bitMask;
//			}
//		}
//		/// <summary>
//		/// Draws a line using Bresenham's line algorithm with **already clipped** coordinates.
//		/// Assumes x1, y1, x2, y2 are within the screen bounds (0-255 for X, 0-191 for Y).
//		/// </summary>
//		/// <param name="zxRam">The byte array representing the ZX Spectrum RAM, where index 0 is $4000.</param>
//		/// <param name="yLineOffsets">The pre-calculated array of Y-line memory offsets.</param>
//		/// <param name="x1">The starting X-coordinate (0-255).</param>
//		/// <param name="y1">The starting Y-coordinate (0-191).</param>
//		/// <param name="x2">The ending X-coordinate (0-255).</param>
//		/// <param name="y2">The ending Y-coordinate (0-191).</param>
//		/// <param name="setPixel">True to draw the line in foreground color, false to erase in background color.</param>
//		private static void DrawLineBresenhamClipped(byte[] zxRam, int[] yLineOffsets, int x1, int y1, int x2, int y2, bool setPixel)
//		{
//			// Note: No clamping here. Coordinates are expected to be within bounds.
//			int dx = Math.Abs(x2 - x1);
//			int dy = Math.Abs(y2 - y1);
//			int sx = (x1 < x2) ? 1 : -1;
//			int sy = (y1 < y2) ? 1 : -1;
//			int err = dx - dy;

//			while (true)
//			{
//				Plot(zxRam, yLineOffsets, (byte)x1, (byte)y1, setPixel);

//				if (x1 == x2 && y1 == y2)
//				{
//					break;
//				}

//				int e2 = 2 * err;
//				if (e2 > -dy)
//				{
//					err -= dy;
//					x1 += sx;
//				}
//				if (e2 < dx)
//				{
//					err += dx;
//					y1 += sy;
//				}
//			}
//		}
//		public static void DrawLineLiangBarsky(byte[] zxRam, int[] yLineOffsets, int x1, int y1, int x2, int y2, bool setPixel)
//		{
//			if (zxRam == null)
//			{
//				throw new ArgumentNullException(nameof(zxRam), "ZX Spectrum RAM array cannot be null.");
//			}
//			if (yLineOffsets == null)
//			{
//				throw new ArgumentNullException(nameof(yLineOffsets), "Y-line offsets table cannot be null.");
//			}
//			if (yLineOffsets.Length != 192)
//			{
//				throw new ArgumentException("The Y-line offsets table must contain exactly 192 elements.", nameof(yLineOffsets));
//			}

//			const int xMin = 0;
//			const int intYMin = 0;
//			const int xMax = 255;
//			const int intYMax = 191;

//			double t0 = 0.0;
//			double t1 = 1.0;

//			double dx = x2 - x1;
//			double dy = y2 - y1;

//			double[] p = { -dx, dx, -dy, dy };
//			double[] q = { x1 - xMin, xMax - x1, y1 - intYMin, intYMax - y1 };

//			for (int i = 0; i < 4; i++)
//			{
//				if (p[i] == 0)
//				{
//					if (q[i] < 0)
//					{
//						return;
//					}
//				}
//				else
//				{
//					double t = q[i] / p[i];
//					if (p[i] < 0)
//					{
//						t0 = Math.Max(t0, t);
//					}
//					else
//					{
//						t1 = Math.Min(t1, t);
//					}
//				}
//			}

//			if (t0 > t1)
//			{
//				return;
//			}

//			int clippedX1 = (int)(x1 + t0 * dx);
//			int clippedY1 = (int)(y1 + t0 * dy);
//			int clippedX2 = (int)(x1 + t1 * dx);
//			int clippedY2 = (int)(y1 + t1 * dy);

//			DrawLineBresenhamClipped(zxRam, yLineOffsets, clippedX1, clippedY1, clippedX2, clippedY2, setPixel);
//		}
//		/// <summary>
//		/// Helper method to draw 8 symmetrical points of a circle, with clipping.
//		/// </summary>
//		/// <param name="zxRam">The byte array representing the ZX Spectrum RAM.</param>
//		/// <param name="yLineOffsets">The pre-calculated array of Y-line memory offsets.</param>
//		/// <param name="xc">Center X coordinate.</param>
//		/// <param name="yc">Center Y coordinate.</param>
//		/// <param name="x">Current X offset from center.</param>
//		/// <param name="y">Current Y offset from center.</param>
//		/// <param name="setPixel">True to set, false to reset the pixel.</param>
//		private static void PlotCirclePoints(byte[] zxRam, int[] yLineOffsets, int xc, int yc, int x, int y, bool setPixel)
//		{
//			// The screen bounds for clipping individual points
//			const int xMin = 0;
//			const int yMin = 0;
//			const int xMax = 255;
//			const int yMax = 191;

//			// Plot 8 symmetrical points, applying boundary checks for each
//			if (xc + x >= xMin && xc + x <= xMax && yc + y >= yMin && yc + y <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc + x), (byte)(yc + y), setPixel);
//			}
//			if (xc - x >= xMin && xc - x <= xMax && yc + y >= yMin && yc + y <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc - x), (byte)(yc + y), setPixel);
//			}
//			if (xc + x >= xMin && xc + x <= xMax && yc - y >= yMin && yc - y <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc + x), (byte)(yc - y), setPixel);
//			}
//			if (xc - x >= xMin && xc - x <= xMax && yc - y >= yMin && yc - y <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc - x), (byte)(yc - y), setPixel);
//			}
//			if (xc + y >= xMin && xc + y <= xMax && yc + x >= yMin && yc + x <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc + y), (byte)(yc + x), setPixel);
//			}
//			if (xc - y >= xMin && xc - y <= xMax && yc + x >= yMin && yc + x <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc - y), (byte)(yc + x), setPixel);
//			}
//			if (xc + y >= xMin && xc + y <= xMax && yc - x >= yMin && yc - x <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc + y), (byte)(yc - x), setPixel);
//			}
//			if (xc - y >= xMin && xc - y <= xMax && yc - x >= yMin && yc - x <= yMax)
//			{
//				Plot(zxRam, yLineOffsets, (byte)(xc - y), (byte)(yc - x), setPixel);
//			}
//		}


//		/// <summary>
//		/// Draws a circle using the Midpoint Circle Algorithm.
//		/// Pixels are only plotted if they fall within the ZX Spectrum screen bounds.
//		/// </summary>
//		/// <param name="zxRam">The byte array representing the ZX Spectrum RAM, where index 0 is $4000.</param>
//		/// <param name="yLineOffsets">The pre-calculated array of Y-line memory offsets.</param>
//		/// <param name="xc">The X-coordinate of the circle's center (0-255).</param>
//		/// <param name="yc">The Y-coordinate of the circle's center (0-191).</param>
//		/// <param name="r">The radius of the circle.</param>
//		/// <param name="setPixel">True to draw the circle, false to erase it.</param>
//		public static void DrawCircleMidpoint(byte[] zxRam, int[] yLineOffsets, int xc, int yc, int r, bool setPixel)
//		{
//			// Initial parameter validation.
//			if (zxRam == null)
//			{
//				throw new ArgumentNullException(nameof(zxRam), "ZX Spectrum RAM array cannot be null.");
//			}
//			if (yLineOffsets == null)
//			{
//				throw new ArgumentNullException(nameof(yLineOffsets), "Y-line offsets table cannot be null.");
//			}
//			if (yLineOffsets.Length != 192)
//			{
//				throw new ArgumentException("The Y-line offsets table must contain exactly 192 elements.", nameof(yLineOffsets));
//			}
//			if (r < 0)
//			{
//				return; // A negative radius doesn't make sense for drawing, silently return.
//			}

//			int x = 0;
//			int y = r;
//			int p = 1 - r; // Decision parameter

//			PlotCirclePoints(zxRam, yLineOffsets, xc, yc, x, y, setPixel);

//			while (x < y)
//			{
//				x++;
//				if (p < 0)
//				{
//					p += 2 * x + 1;
//				}
//				else
//				{
//					y--;
//					p += 2 * (x - y) + 1;
//				}
//				PlotCirclePoints(zxRam, yLineOffsets, xc, yc, x, y, setPixel);
//			}
//		}
//		/// <summary>
//		/// Scrolls the ZX Spectrum video RAM vertically (up or down).
//		/// </summary>
//		/// <param name="zxRam">The byte array representing the ZX Spectrum RAM, where index 0 is $4000.</param>
//		/// <param name="yLineOffsets">The pre-calculated array of Y-line memory offsets.</param>
//		/// <param name="scrollAmount">The number of pixels to scroll. Positive for down, negative for up (1, 2, 4, 8, 16, 32, 64).</param>
//		public static void ScrollVertical(byte[] zxRam, int[] yLineOffsets, int scrollAmount)
//		{
//			// Input validation
//			if (zxRam == null)
//			{
//				throw new ArgumentNullException(nameof(zxRam), "ZX Spectrum RAM array cannot be null.");
//			}
//			if (yLineOffsets == null)
//			{
//				throw new ArgumentNullException(nameof(yLineOffsets), "Y-line offsets table cannot be null.");
//			}
//			if (yLineOffsets.Length != 192)
//			{
//				throw new ArgumentException("The Y-line offsets table must contain exactly 192 elements.", nameof(yLineOffsets));
//			}

//			// Ensure scrollAmount is one of the allowed values
//			if (!IsAllowedScrollAmount(Math.Abs(scrollAmount)))
//			{
//				throw new ArgumentOutOfRangeException(nameof(scrollAmount), "Scroll amount must be 1, 2, 4, 8, 16, 32, or 64 pixels.");
//			}

//			int absScrollAmount = Math.Abs(scrollAmount);

//			// Determine direction and iteration bounds
//			if (scrollAmount > 0) // Scroll Down
//			{
//				// Copy from bottom to top to avoid overwriting data before it's moved
//				for (int y = 191; y >= absScrollAmount; y--)
//				{
//					Buffer.BlockCopy(zxRam, yLineOffsets[y - absScrollAmount], zxRam, yLineOffsets[y], SCREEN_WIDTH_BYTES);
//				}
//				// Clear the top 'absScrollAmount' lines
//				for (int y = 0; y < absScrollAmount; y++)
//				{
//					Array.Clear(zxRam, yLineOffsets[y], SCREEN_WIDTH_BYTES);
//				}
//			}
//			else // Scroll Up
//			{
//				// Copy from top to bottom to avoid overwriting data before it's moved
//				for (int y = 0; y < 192 - absScrollAmount; y++)
//				{
//					Buffer.BlockCopy(zxRam, yLineOffsets[y + absScrollAmount], zxRam, yLineOffsets[y], SCREEN_WIDTH_BYTES);
//				}
//				// Clear the bottom 'absScrollAmount' lines
//				for (int y = 192 - absScrollAmount; y < 192; y++)
//				{
//					Array.Clear(zxRam, yLineOffsets[y], SCREEN_WIDTH_BYTES);
//				}
//			}
//		}

//		/// <summary>
//		/// Scrolls the ZX Spectrum video RAM horizontally (left or right).
//		/// Handles both byte-aligned and bit-shifted scrolling.
//		/// </summary>
//		/// <param name="zxRam">The byte array representing the ZX Spectrum RAM, where index 0 is $4000.</param>
//		/// <param name="yLineOffsets">The pre-calculated array of Y-line memory offsets.</param>
//		/// <param name="scrollAmount">The number of pixels to scroll. Positive for right, negative for left (1, 2, 4, 8, 16, 32, 64).</param>
//		public static void ScrollHorizontal(byte[] zxRam, int[] yLineOffsets, int scrollAmount)
//		{
//			// Input validation
//			if (zxRam == null)
//			{
//				throw new ArgumentNullException(nameof(zxRam), "ZX Spectrum RAM array cannot be null.");
//			}
//			if (yLineOffsets == null)
//			{
//				throw new ArgumentNullException(nameof(yLineOffsets), "Y-line offsets table cannot be null.");
//			}
//			if (yLineOffsets.Length != 192)
//			{
//				throw new ArgumentException("The Y-line offsets table must contain exactly 192 elements.", nameof(yLineOffsets));
//			}

//			// Ensure scrollAmount is one of the allowed values
//			if (!IsAllowedScrollAmount(Math.Abs(scrollAmount)))
//			{
//				throw new ArgumentOutOfRangeException(nameof(scrollAmount), "Scroll amount must be 1, 2, 4, 8, 16, 32, or 64 pixels.");
//			}

//			int absScrollAmount = Math.Abs(scrollAmount);
//			int byteShift = absScrollAmount / 8; // Number of full bytes to shift
//			int bitShift = absScrollAmount % 8;  // Number of bits to shift within bytes

//			// Temporary buffer for line data to avoid overwriting during shift operations
//			// A small optimization: use a single buffer per call, sized to a screen line.
//			byte[] lineBuffer = new byte[SCREEN_WIDTH_BYTES];

//			// Iterate through each visible Y-line
//			for (int y = 0; y < 192; y++)
//			{
//				int lineStartOffset = yLineOffsets[y];

//				// 1. Read current line data into a temporary buffer
//				Buffer.BlockCopy(zxRam, lineStartOffset, lineBuffer, 0, SCREEN_WIDTH_BYTES);

//				// 2. Clear the original line in RAM (to ensure blank areas are truly blank)
//				Array.Clear(zxRam, lineStartOffset, SCREEN_WIDTH_BYTES);

//				// 3. Perform the actual scroll into the original RAM location

//				if (scrollAmount > 0) // Scroll Right
//				{
//					if (bitShift == 0) // Byte-aligned scroll (8, 16, 32, 64 pixels)
//					{
//						for (int i = SCREEN_WIDTH_BYTES - 1; i >= byteShift; i--)
//						{
//							zxRam[lineStartOffset + i] = lineBuffer[i - byteShift];
//						}
//					}
//					else // Bit-shifted scroll (1, 2, 4 pixels)
//					{
//						int rightBitShift = bitShift;
//						int leftBitShift = 8 - bitShift;

//						for (int i = SCREEN_WIDTH_BYTES - 1; i >= byteShift; i--)
//						{
//							byte currentByte = lineBuffer[i - byteShift];
//							byte nextByte = (i - byteShift - 1 >= 0) ? lineBuffer[i - byteShift - 1] : (byte)0;

//							// Shift current byte right, and bring in bits from the left (previous byte)
//							zxRam[lineStartOffset + i] = (byte)((currentByte >> rightBitShift) | (nextByte << leftBitShift));
//						}
//					}
//				}
//				else // Scroll Left
//				{
//					if (bitShift == 0) // Byte-aligned scroll (8, 16, 32, 64 pixels)
//					{
//						for (int i = 0; i < SCREEN_WIDTH_BYTES - byteShift; i++)
//						{
//							zxRam[lineStartOffset + i] = lineBuffer[i + byteShift];
//						}
//					}
//					else // Bit-shifted scroll (1, 2, 4 pixels)
//					{
//						int leftBitShift = bitShift;
//						int rightBitShift = 8 - bitShift;

//						for (int i = 0; i < SCREEN_WIDTH_BYTES - byteShift; i++)
//						{
//							byte currentByte = lineBuffer[i + byteShift];
//							byte prevByte = (i + byteShift + 1 < SCREEN_WIDTH_BYTES) ? lineBuffer[i + byteShift + 1] : (byte)0;

//							// Shift current byte left, and bring in bits from the right (next byte)
//							zxRam[lineStartOffset + i] = (byte)((currentByte << leftBitShift) | (prevByte >> rightBitShift));
//						}
//					}
//				}
//			}
//		}

//		/// <summary>
//		/// Helper method to validate scroll amounts.
//		/// </summary>
//		private static bool IsAllowedScrollAmount(int amount)
//		{
//			return amount == 1 || amount == 2 || amount == 4 || amount == 8 ||
//				   amount == 16 || amount == 32 || amount == 64;
//		}
//	}
//}
