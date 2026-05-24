namespace Speccy.Devices.ULAZX
{
	partial class LokeyDebugDevice
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
			this.tb_debug = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// tb_debug
			// 
			this.tb_debug.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tb_debug.Location = new System.Drawing.Point(13, 13);
			this.tb_debug.Multiline = true;
			this.tb_debug.Name = "tb_debug";
			this.tb_debug.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.tb_debug.Size = new System.Drawing.Size(735, 592);
			this.tb_debug.TabIndex = 0;
			// 
			// DebugWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(757, 617);
			this.Controls.Add(this.tb_debug);
			this.Name = "DebugWindow";
			this.Text = "DebugWindow";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox tb_debug;
	}
}