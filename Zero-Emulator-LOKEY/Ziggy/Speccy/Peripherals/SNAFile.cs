using System;
using System.Runtime.InteropServices;

namespace Peripherals
{
	public class ByteUtililty
	{
		public static byte[] RawSerialize(object anything)
		{
			int rawsize = Marshal.SizeOf(anything);
			IntPtr buffer = Marshal.AllocHGlobal(rawsize);
			Marshal.StructureToPtr(anything, buffer, false);
			byte[] rawdatas = new byte[rawsize];
			Marshal.Copy(buffer, rawdatas, 0, rawsize);
			Marshal.FreeHGlobal(buffer);
			return rawdatas;
		}

		/// <summary>
		/// converts byte[] to struct
		/// </summary>
		public static T RawDeserialize<T>(byte[] rawData, int position)
		{
			int rawsize = Marshal.SizeOf(typeof(T));
			if (rawsize > rawData.Length - position)
				throw new ArgumentException("Not enough data to fill struct. Array length from position: " + (rawData.Length - position) + ", Struct length: " + rawsize);
			IntPtr buffer = Marshal.AllocHGlobal(rawsize);
			Marshal.Copy(rawData, position, buffer, rawsize);
			T retobj = (T)Marshal.PtrToStructure(buffer, typeof(T));
			Marshal.FreeHGlobal(buffer);
			return retobj;
		}
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SNA_HEADER
    {
        public byte I;                  //I Register
        public ushort HL_, DE_, BC_, AF_;  //Alternate registers
        public ushort HL, DE, BC, IY, IX;  //16 bit main registers
        public byte IFF2;               //Interrupt enabled? (bit 2 on/off)
        public byte R;                  //R Register
        public ushort AF, SP;              //AF and SP register
        public byte IM;                 //Interupt Mode
        public byte BORDER;             //Border colour
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SNA_SNAPSHOT
    {
        public byte TYPE;                      //0 = 48k, 1 = 128;
        public SNA_HEADER HEADER;              //The above header
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SNA_48K : SNA_SNAPSHOT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 49152)]
        public byte[] RAM;              //Contents of the RAM
    }

    public class SNAFile
    {
        //Will return a filled snapshot structure from buffer
        public static SNA_SNAPSHOT LoadSNA(ref byte[] buffer)
		{
            SNA_SNAPSHOT snapshot;
   
            if (buffer.Length == 0)
                return null; //something bad happened!

            if (buffer.Length == 49179)
			{
                snapshot = new SNA_48K();
                snapshot.TYPE = 0;
            }
            else
                return null;

            snapshot.HEADER.I = buffer[0];
            snapshot.HEADER.HL_ = (ushort)(buffer[1] | (buffer[2] << 8));
            snapshot.HEADER.DE_ = (ushort)(buffer[3] | (buffer[4] << 8));
            snapshot.HEADER.BC_ = (ushort)(buffer[5] | (buffer[6] << 8));
            snapshot.HEADER.AF_ = (ushort)(buffer[7] | (buffer[8] << 8));

            snapshot.HEADER.HL = (ushort)(buffer[9] | (buffer[10] << 8));
            snapshot.HEADER.DE = (ushort)(buffer[11] | (buffer[12] << 8));
            snapshot.HEADER.BC = (ushort)(buffer[13] | (buffer[14] << 8));
            snapshot.HEADER.IY = (ushort)(buffer[15] | (buffer[16] << 8));
            snapshot.HEADER.IX = (ushort)(buffer[17] | (buffer[18] << 8));

            snapshot.HEADER.IFF2 = buffer[19];
            snapshot.HEADER.R = buffer[20];
            snapshot.HEADER.AF = (ushort)(buffer[21] | (buffer[22] << 8));
            snapshot.HEADER.SP = (ushort)(buffer[23] | (buffer[24] << 8));
            snapshot.HEADER.IM = buffer[25];
            snapshot.HEADER.BORDER = (byte)(buffer[26] & 0x07);

            //48k snapshot
            if (snapshot.TYPE == 0)
			{
                ((SNA_48K)snapshot).RAM = new byte[49152];
                Array.Copy(buffer, 27, ((SNA_48K)snapshot).RAM, 0, 49152);
            }
            return snapshot;
        }

        //Will return a filled snapshot structure from file stream
        public static SNA_SNAPSHOT LoadSNA(System.IO.Stream fs)
		{
            SNA_SNAPSHOT snapshot;
            using (System.IO.BinaryReader r = new System.IO.BinaryReader(fs)) {
                byte[] buffer = new byte[fs.Length];
                int bytesRead = r.Read(buffer, 0, (int)fs.Length);
                snapshot = LoadSNA(ref buffer);
            }
            return snapshot;
        }

        //Will return a filled snapshot structure from file
        public static SNA_SNAPSHOT LoadSNA(string filename)
		{
            SNA_SNAPSHOT sna;
            using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open)) {
                byte[] buffer = new byte[fs.Length];
                int bytesRead = fs.Read(buffer, 0, (int)fs.Length);
                sna = LoadSNA(ref buffer);
            }
            return sna;
        }

		//ZXIT - Will return a filled snapshot structure from byte[]
		public static SNA_SNAPSHOT LoadSNA(byte[] snabuffer)
		{
			return LoadSNA(ref snabuffer);
		}

		public static void SaveSNA(string filename, SNA_SNAPSHOT sna) {

            using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create)) {

                byte[] bytes = ByteUtililty.RawSerialize(sna.HEADER);
                fs.Write(bytes, 0, bytes.Length);

                if (sna is SNA_48K)
                        fs.Write(((SNA_48K)sna).RAM, 0, ((SNA_48K)sna).RAM.Length);
                
            }
        }
    }
}