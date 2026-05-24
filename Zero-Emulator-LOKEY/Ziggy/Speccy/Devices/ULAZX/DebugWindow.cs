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
	public partial class LokeyDebugDevice : Form, ILokeyDebugDevice
	{
		public LokeyDebugDevice()
		{
			InitializeComponent();
		}

		public void DebugClear()
		{
			tb_debug.Clear();
			tb_debug.Invalidate();
		}

		public void DebugDispose()
		{
			this.Hide();
		}

		public void DeviceHide()
		{
			this.Hide();
			this.Invalidate();
		}

		public void DebugOut(string debugout)
		{
			tb_debug.AppendText(debugout+Environment.NewLine);
			tb_debug.ScrollToCaret();
			tb_debug.Invalidate();
		}

		public void DeviceShow()
		{
			this.Show();
			this.Invalidate();
		}
	}
}
