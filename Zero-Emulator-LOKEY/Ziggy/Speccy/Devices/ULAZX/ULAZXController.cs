using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LOKEY;

namespace Speccy.Devices.ULAZX
{
	public partial class LokeyControllerDevice : Form, ILokeyControllerDevice
	{

		public Func<int,string, bool> LoadRom { get; set; }
		public Func<string, bool> LoadDisk { get; set; }
		public LokeyControllerDevice()
		{
			InitializeComponent();
		}

		private void bt_ROM0_Click(object sender, EventArgs e)
		{
			InsertRom(0);
		}

		private void bt_ROM1_Click(object sender, EventArgs e)
		{
			InsertRom(1);
		}
		private void bt_DISK_Click(object sender, EventArgs e)
		{
			InsertDisk();
		}
		private void InsertRom(int rom)
		{
			DialogResult res = opfd_loadrom.ShowDialog();
			if (res == DialogResult.OK)
			{
				string filename = opfd_loadrom.FileName;
				bool didload = LoadRom(rom, filename);
				if (didload)
				{
					switch (rom)
					{
						case 0:
							tb_ROM0.Text = filename;
							tb_ROM0.BackColor = Color.Lime;
							break;
						case 1:
							tb_ROM1.Text = filename;
							tb_ROM1.BackColor = Color.Lime;
							break;
					}
				}
			}
		}

		public string GetRomFile(int bank)
		{
			switch (bank)
			{
				case 0:
					return tb_ROM0.Text;
					break;
				case 1:
					return tb_ROM1.Text;
					break;
			}
			return string.Empty;
		}

		public void SetRomFile(int bank, string file)
		{
			throw new NotImplementedException();
		}
		private void InsertDisk()
		{
			DialogResult res = opfd_loaddisk.ShowDialog();
			if (res == DialogResult.OK)
			{
				string filename = opfd_loaddisk.FileName;
				bool didload = LoadDisk(filename);
				if (didload)
				{
					tb_disk.Text = filename;
					tb_disk.BackColor = Color.Lime;
				}
			}
		}
		public string GetDiskFile()
		{
			throw new NotImplementedException();
		}

		public void SetDiskFile(string file)
		{
			throw new NotImplementedException();
		}

		public void DeviceShow()
		{
			this.Visible = true;
		}

		public void DeviceHide()
		{
			this.Visible=false;
		}
	}
}
