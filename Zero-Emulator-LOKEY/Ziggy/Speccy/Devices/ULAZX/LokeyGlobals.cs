using Speccy;
using Speccy.Devices.ULAZX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LOKEY
{
	public static class LokeyGlobals
	{
		//ZXIT global debug device
		public static ILokeyDebugDevice LokeyDebug { get;} = new LokeyDebugDevice();
		//ULAZX controller window
		public static ILokeyControllerDevice LokeyController { get; } = new LokeyControllerDevice();
		public static IList<byte> HostRAM { get; set; }
		public static readonly double lokey_cpu_multiplier = 1.25;
	}
	public class ZxRam : IList<byte>
	{
		private zx_spectrum _host = null;
		public ZxRam(zx_spectrum host)
		{
			_host = host;
		}
		public byte this[int index]
		{
			get => _host.PeekByteNoContend((ushort)index);
			set => _host.PokeByteNoContend((ushort)index, value);
		}
		public int Count => 64 * 1024;
		public IEnumerator<byte> GetEnumerator()
		{
			for (int i = 0; i < Count; i++) yield return Get(i);
		}
		public byte Get(int index) => _host.PeekByteNoContend((ushort)index);
		public void Set(int index, byte value) { _host.PokeByteNoContend((ushort)index, value); }
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
	}
}
