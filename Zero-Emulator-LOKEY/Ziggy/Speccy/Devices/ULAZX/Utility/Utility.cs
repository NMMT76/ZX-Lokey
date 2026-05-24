using Speccy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LOKEY
{
	public class LokeyDiskDrive : IList<byte>
	{
		private FileStream _filestream;
		private string _file;
		public LokeyDiskDrive()
		{
		}
		public bool Load(string file)
		{
			_file = file;
			if (File.Exists(_file))
			{
				try
				{
					_filestream = new FileStream(_file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
					return true;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					return false;
				}
			}
			else
			{
				return false;
			}
		}
		public void Unload()
		{
			_filestream.Flush();
			_filestream.Dispose();
		}
		public byte this[int index]
		{
			get => Get((ushort)index);
			set => Set((ushort)index, value);
		}
		public int Count => 64 * 1024;
		public IEnumerator<byte> GetEnumerator()
		{
			for (int i = 0; i < Count; i++) yield return Get(i);
		}
		public byte Get(int index)
		{
			if (_filestream != null)
			{
				_filestream.Seek(index, SeekOrigin.Begin);
				return (byte)(_filestream.ReadByte());
			}
			else
			{
				return 0;
			}
		}
		public void Set(int index, byte value)
		{
			if (_filestream != null)
			{
				_filestream.Seek(index, SeekOrigin.Begin);
				_filestream.WriteByte(value);
				_filestream.Flush();
			}
		}
		public bool IsReadOnly => false;
		public void Add(byte item) => throw new NotSupportedException();
		public void Clear() => throw new NotSupportedException();
		public bool Contains(byte item) => throw new NotImplementedException();
		public void CopyTo(byte[] array, int arrayIndex) => throw new NotImplementedException();
		public int IndexOf(byte item) => throw new NotImplementedException();
		public void Insert(int index, byte item) => throw new NotImplementedException();
		public bool Remove(byte item) => throw new NotImplementedException();
		public void RemoveAt(int index) => throw new NotImplementedException();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		public byte[] GetSna()
		{
			byte[] sna = new byte[49179];
			if (_filestream != null && _filestream.Length >= 49179)
			{
				_filestream.Position = 0;
				_filestream.Read(sna, 0, 49179);
			}
			return sna;
		}
	}
	
	public enum ELOKEY_ERRORCODE
	{
		NONE = 0, ARGNUM, ARGPARM, ARGVALUE, EMPTYROM, SRAMADROOB, ROMADDROOB, ATTRMODEBAD
	}
	public enum ScrollDirection
	{
		Up = 1,
		UpRight,
		Right,
		DownRight,
		Down,
		DownLeft,
		Left,
		UpLeft
	}
	public interface ILokeyDeviceUI
	{
		void DeviceShow();
		void DeviceHide();
	}
	public interface ILokeyControllerDevice : ILokeyDeviceUI
	{
		Func<int, string, bool> LoadRom { get; set; }
		Func<string, bool> LoadDisk { get; set; }
		string GetRomFile(int bank);
		void SetRomFile(int bank, string file);
		string GetDiskFile();
		void SetDiskFile(string file);
	}
	public interface ILokeyDebugDevice : ILokeyDeviceUI
	{
		void DebugOut(string debugout);
		void DebugClear();
		void DebugDispose();
	}
	public static class CopperPaletteGenerator
	{
		// One dictionary to hold all palettes, keyed by intensity change (-100 to +100)
		public static int[][] IntensityPalettes { get; private set; } = new int[256][];
		public static int[][] ColorPalettes { get; private set; } = new int[8][];

		private const byte DEFAULT_ON_VALUE = 255;    // 100% of 255 for the starting "bright" colors

		// The base 8 colors represented by their RGB components ON/OFF state (0 or 1)
		private static readonly byte[][] _baseColorStates = new byte[][]
		{
			new byte[] { 0, 0, 0 }, // Black (all off)
            new byte[] { 0, 0, 1 }, // Blue (B on)
            new byte[] { 1, 0, 0 }, // Red (R on)
            new byte[] { 1, 0, 1 }, // Magenta (R on, B on)
            new byte[] { 0, 1, 0 }, // Green (G on)
            new byte[] { 0, 1, 1 }, // Cyan (G on, B on)
            new byte[] { 1, 1, 0 }, // Yellow (R on, G on)
            new byte[] { 1, 1, 1 }  // White (all on)
        };

		static CopperPaletteGenerator()
		{
			GenerateAllPalettes();
			//var k = Palettes.Count;
		}

		private static void GenerateAllPalettes()
		{
			int[][] intensitypalettes = new int[256][];
			int[][] colorpalettes=new int[8][];
			byte[,] palette100 = new byte[8, 3]
			{
				{0,0,0},{0,0,255},{255,0,0},{255,0,255},{0,255,0},{0,255,255},{255,255,0},{255,255,255}
			};
			intensitypalettes[0] = new int[8];
			//Palette with no changes is the original "non bright" palette
			for (int idx = 0; idx < 8; idx++)
			{
				int tr = palette100[idx, 0], tg = palette100[idx, 1], tb = palette100[idx, 2];
				intensitypalettes[0][idx] = tr * 65536 + tg * 256 + tb;
			}
			//create the "darker" palettes
			for (int c = 1; c < 256; c++)
			{
				int[] temp = new int[8];
				for (int idx = 0; idx < 8; idx++)
				{
					int tr = 0, tg = 0, tb = 0;
					if (palette100[idx, 0] == 255)
					{
						tr = (byte)(255 - c);
					}
					if (palette100[idx, 1] == 255)
					{
						tg = (byte)(255 - c);
					}
					if (palette100[idx, 2] == 255)
					{
						tb = (byte)(255 - c);
					}
					temp[idx] = tr * 65536 + tg * 256 + tb;
				}
				//There's no "darker black...
				temp[0] = 0;
				intensitypalettes[c]=temp;
			}
			IntensityPalettes = intensitypalettes;
			//
			byte[] _gradient = new byte[8] { 0, 36, 73, 109, 146, 182, 219, 255 };
			//
			for (int c = 0; c < 8; c++)
			{
				int[] temp = new int[8];
				for (int idx = 0; idx < 8; idx++)
				{
					int tr = 0, tg = 0, tb = 0;
					if (palette100[c, 0] == 255)
					{
						tr = _gradient[idx];
					}
					if (palette100[c, 1] == 255)
					{
						tg = _gradient[idx];
					}
					if (palette100[c, 2] == 255)
					{
						tb = _gradient[idx];
					}
					temp[idx] = tr * 65536 + tg * 256 + tb;
				}
				colorpalettes[c]=temp;
			}
			ColorPalettes= colorpalettes;
		}
		public static int RemapValue(float value, float oldMax, float newMax)
		{
			// 1. Normalize value to 0.0 - 1.0
			float normalized = value / oldMax;
			// 2. Scale to new range
			float result = normalized * newMax;
			// 3. Round to nearest integer
			return (int)Math.Round(result);
		}
	}
}
