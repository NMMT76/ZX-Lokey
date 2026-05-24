using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ZeroWin
{
	partial class GDIRenderer
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// ZRenderer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.CausesValidation = false;
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "GDIRenderer";
			this.ResumeLayout(false);

		}

		#endregion
	}
	public partial class GDIRenderer : UserControl
	{
		public ZXForm ziggyWin;
		private int screenWidth;
		private int screenHeight;
		private Rectangle screenRect;

		private Bitmap backBuffer;
		private bool isRendering = false;
		private bool doRender = false;
		private bool isSuspended = false;
		private System.Threading.Thread renderThread;

		public int averageFPS = 0;
		private double lastTime = 0;
		private int frameCount = 0;
		private double totalFrameTime = 0;

		public GDIRenderer(ZXForm zw, int width, int height)
		{
			this.ziggyWin = zw;

			// Optimization: Prevent the OS from ever touching the background
			this.SetStyle(ControlStyles.UserPaint |
						  ControlStyles.AllPaintingInWmPaint |
						  ControlStyles.OptimizedDoubleBuffer |
						  ControlStyles.Opaque, true);

			InitializeComponent();
			SetSize(width, height);
			Start();
		}

		public void SetSize(int width, int height)
		{
			this.ClientSize = new Size(width, height);
			screenWidth = ziggyWin.zx.GetTotalScreenWidth();
			screenHeight = ziggyWin.zx.GetTotalScreenHeight();
			screenRect = new Rectangle(0, 0, screenWidth, screenHeight);

			if (backBuffer != null) backBuffer.Dispose();
			backBuffer = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
		}

		private void Start()
		{
			if (renderThread != null && renderThread.IsAlive) return;

			doRender = true;
			isSuspended = false;
			renderThread = new System.Threading.Thread(RenderLoop);
			renderThread.Name = "GDI Render Thread";
			renderThread.Priority = System.Threading.ThreadPriority.BelowNormal;
			renderThread.Start();
		}

		public void Suspend()
		{
			if (isSuspended) return;

			doRender = false;
			if (renderThread != null && renderThread.IsAlive)
			{
				renderThread.Join(500); // Wait up to 500ms for thread to exit cleanly
			}

			isRendering = false;
			isSuspended = true;
		}

		public void Resume()
		{
			if (!isSuspended) return;
			Start();
		}

		public void Shutdown()
		{
			doRender = false;
			if (renderThread != null && renderThread.IsAlive)
				renderThread.Join();

			if (backBuffer != null) backBuffer.Dispose();
		}

		private void RenderLoop()
		{
			while (doRender)
			{
				// check if emulator core has a new frame ready
				if (ziggyWin.zx.needsPaint && !isRendering)
				{
					lock (ziggyWin.zx.lockThis)
					{
						ziggyWin.zx.needsPaint = false;
						isRendering = true;

						// Update the persistent backbuffer
						UpdateBackBuffer();
					}

					// Request a redraw on the UI thread
					this.Invalidate();

					// FPS calculation
					double currentTime = PrecisionTimer.TimeInMilliseconds();
					double frameTime = currentTime - lastTime;
					totalFrameTime += frameTime;
					frameCount++;

					if (totalFrameTime > 1000.0)
					{
						averageFPS = (int)(1000 * frameCount / totalFrameTime);
						frameCount = 0;
						totalFrameTime = 0;
					}
					lastTime = currentTime;
				}
				System.Threading.Thread.Sleep(1);
			}
		}
		private readonly object bitmapLock = new object();
		private void UpdateBackBuffer()
		{
			// Wrap the entire modification in a lock
			lock (bitmapLock)
			{
				if (backBuffer == null) return;

				BitmapData bmpData = backBuffer.LockBits(
					screenRect,
					ImageLockMode.WriteOnly,
					PixelFormat.Format32bppRgb);

				try
				{
					lock (ziggyWin.zx)
					{
						Marshal.Copy(ziggyWin.zx.ScreenBuffer, 0, bmpData.Scan0, screenWidth * screenHeight);
					}
				}
				finally
				{
					backBuffer.UnlockBits(bmpData);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (isSuspended) return;

			// Use the same lock here so we don't draw while the thread is copying data
			lock (bitmapLock)
			{
				if (backBuffer == null) return;

				e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

				// This is where the "In Use" crash usually happens without the lock
				e.Graphics.DrawImage(backBuffer, 0, 0, this.Width, this.Height);
			}

			isRendering = false;
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// Empty to prevent flickering
		}
	}
}