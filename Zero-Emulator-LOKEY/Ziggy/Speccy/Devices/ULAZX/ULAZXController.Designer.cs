namespace Speccy.Devices.ULAZX
{
	partial class LokeyControllerDevice
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.gb_roms = new System.Windows.Forms.GroupBox();
			this.tb_disk = new System.Windows.Forms.TextBox();
			this.tb_ROM1 = new System.Windows.Forms.TextBox();
			this.tb_ROM0 = new System.Windows.Forms.TextBox();
			this.bt_DISK = new System.Windows.Forms.Button();
			this.bt_ROM1 = new System.Windows.Forms.Button();
			this.bt_ROM0 = new System.Windows.Forms.Button();
			this.opfd_loadrom = new System.Windows.Forms.OpenFileDialog();
			this.opfd_loaddisk = new System.Windows.Forms.OpenFileDialog();
			this.gb_roms.SuspendLayout();
			this.SuspendLayout();
			// 
			// gb_roms
			// 
			this.gb_roms.Controls.Add(this.tb_disk);
			this.gb_roms.Controls.Add(this.tb_ROM1);
			this.gb_roms.Controls.Add(this.tb_ROM0);
			this.gb_roms.Controls.Add(this.bt_DISK);
			this.gb_roms.Controls.Add(this.bt_ROM1);
			this.gb_roms.Controls.Add(this.bt_ROM0);
			this.gb_roms.Location = new System.Drawing.Point(13, 13);
			this.gb_roms.Name = "gb_roms";
			this.gb_roms.Size = new System.Drawing.Size(389, 140);
			this.gb_roms.TabIndex = 0;
			this.gb_roms.TabStop = false;
			this.gb_roms.Text = "ROMs";
			// 
			// tb_disk
			// 
			this.tb_disk.Location = new System.Drawing.Point(87, 108);
			this.tb_disk.Name = "tb_disk";
			this.tb_disk.ReadOnly = true;
			this.tb_disk.Size = new System.Drawing.Size(287, 20);
			this.tb_disk.TabIndex = 7;
			// 
			// tb_ROM1
			// 
			this.tb_ROM1.Location = new System.Drawing.Point(87, 50);
			this.tb_ROM1.Name = "tb_ROM1";
			this.tb_ROM1.ReadOnly = true;
			this.tb_ROM1.Size = new System.Drawing.Size(287, 20);
			this.tb_ROM1.TabIndex = 5;
			// 
			// tb_ROM0
			// 
			this.tb_ROM0.Location = new System.Drawing.Point(87, 21);
			this.tb_ROM0.Name = "tb_ROM0";
			this.tb_ROM0.ReadOnly = true;
			this.tb_ROM0.Size = new System.Drawing.Size(287, 20);
			this.tb_ROM0.TabIndex = 4;
			// 
			// bt_DISK
			// 
			this.bt_DISK.Location = new System.Drawing.Point(6, 106);
			this.bt_DISK.Name = "bt_DISK";
			this.bt_DISK.Size = new System.Drawing.Size(75, 23);
			this.bt_DISK.TabIndex = 3;
			this.bt_DISK.Text = "Disk";
			this.bt_DISK.UseVisualStyleBackColor = true;
			this.bt_DISK.Click += new System.EventHandler(this.bt_DISK_Click);
			// 
			// bt_ROM1
			// 
			this.bt_ROM1.Location = new System.Drawing.Point(6, 48);
			this.bt_ROM1.Name = "bt_ROM1";
			this.bt_ROM1.Size = new System.Drawing.Size(75, 23);
			this.bt_ROM1.TabIndex = 1;
			this.bt_ROM1.Text = "ROM1";
			this.bt_ROM1.UseVisualStyleBackColor = true;
			this.bt_ROM1.Click += new System.EventHandler(this.bt_ROM1_Click);
			// 
			// bt_ROM0
			// 
			this.bt_ROM0.Location = new System.Drawing.Point(6, 19);
			this.bt_ROM0.Name = "bt_ROM0";
			this.bt_ROM0.Size = new System.Drawing.Size(75, 23);
			this.bt_ROM0.TabIndex = 0;
			this.bt_ROM0.Text = "ROM0";
			this.bt_ROM0.UseVisualStyleBackColor = true;
			this.bt_ROM0.Click += new System.EventHandler(this.bt_ROM0_Click);
			// 
			// opfd_loadrom
			// 
			this.opfd_loadrom.DefaultExt = "*.rom";
			this.opfd_loadrom.FileName = "openFileDialog1";
			this.opfd_loadrom.Filter = "All|*.rom;*.bin;*.sna;*.scr|Rom|*.rom|Bin|*.bin|Scr|*.scr|Sna|*.sna";
			this.opfd_loadrom.Title = "Load";
			// 
			// opfd_loaddisk
			// 
			this.opfd_loaddisk.DefaultExt = "*.rom";
			this.opfd_loaddisk.FileName = "openFileDialog1";
			this.opfd_loaddisk.Filter = "Disk|*.dsk";
			this.opfd_loaddisk.Title = "Load";
			// 
			// ULAZXController
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(419, 167);
			this.Controls.Add(this.gb_roms);
			this.Name = "ULAZXController";
			this.Text = "ULAZXController";
			this.gb_roms.ResumeLayout(false);
			this.gb_roms.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox gb_roms;
		private System.Windows.Forms.TextBox tb_disk;
		private System.Windows.Forms.TextBox tb_ROM1;
		private System.Windows.Forms.TextBox tb_ROM0;
		private System.Windows.Forms.Button bt_DISK;
		private System.Windows.Forms.Button bt_ROM1;
		private System.Windows.Forms.Button bt_ROM0;
		private System.Windows.Forms.OpenFileDialog opfd_loadrom;
		private System.Windows.Forms.OpenFileDialog opfd_loaddisk;
	}
}