using System.Drawing;
using System.Windows.Forms;

#if RENDERER_SLIMDX
using SlimDX.Direct3D9;
#else

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

#endif

namespace ZeroWin
{

    public partial class ZRenderer : UserControl
    {
        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        private static extern bool StretchBlt(
                                    System.IntPtr hdcDest,
                                   int nXOriginDest,
                                   int nYOriginDest,
                                   int nWidthDest,
                                   int nHeightDest,
                                   System.IntPtr hdcSrc,
                                   int nXOriginSrc,
                                   int nYOriginSrc,
                                   int nWidthSrc,
                                   int nHeightSrc,
                                   int dwRop);

        [System.Runtime.InteropServices.DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern System.IntPtr CreateCompatibleDC(System.IntPtr hDC);

        [System.Runtime.InteropServices.DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern System.IntPtr SelectObject(System.IntPtr hDC, System.IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern System.IntPtr DeleteObject(System.IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern bool DeleteDC(System.IntPtr hdc);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        public static extern int GetWindowDC(int hWnd);

        public ZXForm ziggyWin;

        //private bool useDirectX = true;
		private bool useDirectX = false; //ZXIT
		private bool directXAvailable = false;
        private bool showScanlines = false;
        private System.Threading.Thread renderThread;
        private bool isSuspended = false;
		//private bool pixelSmoothing = true;
		private bool pixelSmoothing = false; //ZXIT
        private bool isRendering = false;
        private bool fullScreenMode = false;
        private Point ledIndicatorPos = new Point(0, 0);
        private double ledBlinkTimer = 0.0f;
        private bool enableVsync = true;
        private double lastTime = 0;
        private double frameTime = 0;
        private double totalFrameTime = 0;
        private int frameCount = 0;
        public int averageFPS = 0;


        public Point LEDIndicatorPosition
		{
            get { return ledIndicatorPos; }
            set {
                ledIndicatorPos = value;
            }
        }
        public bool EnableVsync {
            get { return enableVsync; }
            set { enableVsync = true; }
        }
        public bool PixelSmoothing {
            get { return pixelSmoothing; }
            set { pixelSmoothing = value; }
        }

        public bool DoRender {
            get;
            set;
        }

        public bool DirectXReady
		{
            get { return directXAvailable; }
        }

        public bool EnableDirectX {
            get {
                return useDirectX;
            }
            set {
                useDirectX = value;
                if (useDirectX)
				{
					//this.DoubleBuffered = false;
					this.DoubleBuffered = true; //ZXIT
				}
                else
                {
                    this.DoubleBuffered = true;
                    directXAvailable = false;
                }
            }
        }

        public bool ShowScanlines { get { return showScanlines; } set { showScanlines = value; } }

        public bool EnableFullScreen { get { return fullScreenMode; } set { fullScreenMode = value; }}

        public bool EmulationIsPaused { get; set; }

        private enum SpriteType
        {
            PLAY_LED,
            RECORD_LED,
            VIDEO1_LED,
            VIDEO2_LED,
            DISK_LED,
            DOWNLOAD_LED,
            SCANLINE,
            TAPE_LED,
            PAUSE_LED,
            LAST
        }

        private Rectangle[] spriteSourceRects = new Rectangle[(int)SpriteType.LAST]
		{
			new Rectangle(0, 0, 16, 16), new Rectangle(16, 0, 16, 16),
			new Rectangle(32, 0, 32, 32), new Rectangle(64, 0, 32, 32),
			new Rectangle(96, 0 , 32, 32), new Rectangle(128, 0, 32, 32),
			new Rectangle(160, 0, 32, 32), new Rectangle(192, 0, 32, 32),
			new Rectangle(224, 0, 32, 32)
        };

        #region DIRECTX_RENDERER

        private Device dxDevice;
        private PresentParameters currentParams;

        private DisplaySprite displaySprite = new DisplaySprite();
        private InterlaceSprite scanlineSprite = new InterlaceSprite();

        private Rectangle screenRect;
        private Rectangle displayRect;

        private System.Drawing.Bitmap gdiDisplay;
        private System.Drawing.Bitmap interlaceDisplay;

        private int displayWidth = 256 + 48 + 48;
        private int displayHeight = 192 + 48 + 56;

        private Vector3 spritePos = Vector3.Empty;
        
        public int ScreenWidth { get; set; }

        public int ScreenHeight { get; set; }

        public ZRenderer() {}

        private void DestroyDX()
		{
            scanlineSprite.Destroy();
            displaySprite.Destroy();

            if (gdiDisplay != null)
                gdiDisplay.Dispose();

            if (dxDevice != null)
                dxDevice.Dispose();
        }

        public void Shutdown()
		{
            DestroyDX();
            DoRender = false;
            renderThread.Join();
        }

        public void SetSpeccyScreenSize(int width, int height)
		{
            ScreenWidth = width;
            ScreenHeight = height;
            screenRect = new System.Drawing.Rectangle(0, 0, ScreenWidth, ScreenHeight);
            gdiDisplay = new System.Drawing.Bitmap(ScreenWidth, ScreenHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        }

        public void SetSize(int width, int height)
		{
            if (dxDevice != null)
            {
                displaySprite.Destroy();
                scanlineSprite.Destroy();
                dxDevice.Dispose();
            }

            displayHeight = height;
            displayWidth = width;
            displayRect = new Rectangle(0, 0, displayWidth, displayHeight);

            this.ClientSize = new Size(width, height);
            if (EnableDirectX && !InitDirectX(width, height)) {
                directXAvailable = false;
            }
        }

        public bool InitDirectX(int width, int height, bool is16bit = false)
		{
            isSuspended = true;
            DestroyDX();

            displayRect = new Rectangle(0, 0, width, height);
            displayWidth = width;
            displayHeight = height;

            AdapterInformation adapterInfo = Manager.Adapters.Default;

            ziggyWin.logger.Log("Setting up render parameters...");
            currentParams = new PresentParameters();
            currentParams.BackBufferCount = 2;
            currentParams.BackBufferWidth = width;
            currentParams.BackBufferHeight = height;
            currentParams.SwapEffect = SwapEffect.Discard;
            currentParams.PresentFlag = PresentFlag.None;
            currentParams.PresentationInterval = (enableVsync ? PresentInterval.One : PresentInterval.Immediate);
            currentParams.Windowed = !fullScreenMode;// true;

            Format  currentFormat = Manager.Adapters[0].CurrentDisplayMode.Format;
            bool formatCheck = Manager.CheckDeviceType(0,
                               DeviceType.Hardware,
                               currentFormat,
                               currentFormat,
                               false);

            if (!formatCheck)
                MessageBox.Show("Invalid format", "dx error", MessageBoxButtons.OK);

            if (fullScreenMode)
			{
                currentParams.DeviceWindow = this.Parent;
                currentParams.BackBufferFormat = currentFormat;//(is16bit ? Format.R5G6B5 : Format.X8B8G8R8);
            } else
			{
                currentParams.DeviceWindow = this;
                currentParams.BackBufferFormat = adapterInfo.CurrentDisplayMode.Format;
            }

            try
			{
                ziggyWin.logger.Log("Initializing directX device...");
                dxDevice = new Device(0, DeviceType.Hardware, this, CreateFlags.HardwareVertexProcessing, currentParams);
            } catch (Microsoft.DirectX.DirectXException dx)
			{
                MessageBox.Show(dx.ErrorString, "DX error", MessageBoxButtons.OK);
                try
				{
                    dxDevice = new Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, currentParams);
                } catch (Microsoft.DirectX.DirectXException dx2){
                    MessageBox.Show(dx2.ErrorString, "DX error", MessageBoxButtons.OK);
                    directXAvailable = false;
                    return false;
                }
            }
        
            SetSpeccyScreenSize(ziggyWin.zx.GetTotalScreenWidth(), ziggyWin.zx.GetTotalScreenHeight());

            float scaleX = ((float)displayWidth / (float)(ScreenWidth));
            float scaleY = ((float)displayHeight / (float)(ScreenHeight));

            //Maintain 4:3 aspect ration when full screen
            if (EnableFullScreen && ziggyWin.config.renderOptions.MaintainAspectRatioInFullScreen)
            {
                if (displayHeight < displayWidth)
                {
                    float aspectXScale = 0.75f;
                    scaleX = (scaleX * aspectXScale);
                    int newWidth = (int)(displayWidth * aspectXScale);
                    spritePos = new Vector3((((displayWidth - newWidth)) / (scaleX * 2.0f)), 0, 0);
                }
                else //Not tested!!!
                {
                    float aspectYScale = 1.33f;
                    scaleY = (scaleY * aspectYScale);
                    int newHeight = (int)(displayHeight * aspectYScale);
                }
            }
            else
                spritePos = Vector3.Empty;

            if (scaleX < 1.0f)
                scaleX = 1.0f;

            if (scaleY < 1.0f)
                scaleY = 1.0f;

            Matrix scaling = Matrix.Scaling(scaleX, scaleY, 1.0f);
            //sprite.Transform = scaling;
            System.Console.WriteLine("scaleX " + scaleX + "     scaleY " + scaleY);
            System.Console.WriteLine("pos " + spritePos);
            Texture displayTexture = new Texture(dxDevice, ScreenWidth, ScreenHeight, 1, Usage.None, currentParams.BackBufferFormat, Pool.Managed);

            displaySprite.Init(dxDevice, displayTexture, new Rectangle((int)spritePos.X, (int)spritePos.Y, ScreenWidth, ScreenHeight), scaling);

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream()) {
                ZeroWin.Properties.Resources.scanlines2.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                Texture interlaceTexture = Texture.FromStream(dxDevice, stream, Usage.None, Pool.Managed);
                Surface interlaceSurface = interlaceTexture.GetSurfaceLevel(0);

                //Why 1.5f? Because it works very well. 
                //Trying to use displayHeight/texture_width (which would seem logical) leads to strange banding on the screen.
                scanlineSprite.Init(dxDevice, interlaceTexture, new Rectangle((int)spritePos.X, (int)spritePos.Y, ScreenWidth, ScreenHeight), scaling, 1.0f, displayHeight / 1.5f); 
            }

            System.Drawing.Font systemfont = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily, 10f, FontStyle.Regular);
            isSuspended = false;
            lastTime = PrecisionTimer.TimeInMilliseconds();
            directXAvailable = true;
            return true;
        }

        public ZRenderer(ZXForm zw, int width, int height)
		{
            InitializeComponent();
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            interlaceDisplay = ZeroWin.Properties.Resources.scanlines2;

            ziggyWin = zw;
            displayHeight = height;
            displayWidth = width;

            this.ClientSize = new System.Drawing.Size(width, height);
            if (!InitDirectX(width, height))
			{
                directXAvailable = false;
                dxDevice = null;
            }
            Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
		{
        }

        private void Start()
		{
            renderThread = new System.Threading.Thread(new System.Threading.ThreadStart(RenderDX));
            renderThread.Name = "Render Thread";
            renderThread.Priority = System.Threading.ThreadPriority.Lowest;
            DoRender = true;
            isRendering = false;
            isSuspended = false;
            renderThread.Start();
            ledBlinkTimer = PrecisionTimer.TimeInSeconds();
        }

        public void Suspend()
		{
            if (isSuspended)
                return;
            DoRender = false;
            renderThread.Join();
            isRendering = false;
            isSuspended = true;
        }

        public void Resume() {
            if (!isSuspended)
                return;
            Start();
        }

        private void RenderDDSurface()
		{
            //Sometimes render is attempted even after dxDevice has been disposed off.
            if (dxDevice.Disposed)
			{
                return;
            }

            lock (ziggyWin.zx) {
                displaySprite.CopySurface(screenRect,ziggyWin.zx.ScreenBuffer);
            }

            if (fullScreenMode)
                dxDevice.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Target, Color.Black, 1.0f, 0);

            dxDevice.BeginScene();
            dxDevice.RenderState.Lighting = false;

            displaySprite.Render(dxDevice, (pixelSmoothing ? TextureFilter.Linear : TextureFilter.None), TextureAddress.Border);

			if (showScanlines) { scanlineSprite.Render(dxDevice, TextureFilter.Linear, TextureAddress.Wrap); }

            dxDevice.EndScene();
            int coopLevel;
            dxDevice.CheckCooperativeLevel(out coopLevel);
            ResultCode deviceState = (ResultCode)coopLevel;
            if (deviceState == ResultCode.DeviceLost)
			{
                System.Threading.Thread.Sleep(1);
                return;
            } else if (deviceState == ResultCode.DeviceNotReset)
			{
                SetSize(Width, Height);
                return;
            }

            try
			{
                dxDevice.Present();
            }
            catch (DeviceLostException de) {
                System.Threading.Thread.Sleep(1);
            }
        }

        public void RenderDX()
		{
            while (DoRender)
			{
                if (ziggyWin.zx.needsPaint && !isRendering)
				{
                    lock (ziggyWin.zx.lockThis)
					{
                        ziggyWin.zx.needsPaint = false;
                        isRendering = true;
                        this.Invalidate();
                    }
                    frameTime = PrecisionTimer.TimeInMilliseconds() - lastTime;
                    frameCount++;
                    totalFrameTime += frameTime;

                    if (totalFrameTime > 1000.0f)
                    {
                        averageFPS = (int)(1000 * frameCount / totalFrameTime);
                        frameCount = 0;
                        totalFrameTime = 0;
                    }
                    lastTime = PrecisionTimer.TimeInMilliseconds();
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
		{
            if (isSuspended)
                return;
			if (useDirectX && directXAvailable)
			{
				RenderDDSurface();
			}                
            else
			{
                System.Drawing.Imaging.BitmapData bmpData = gdiDisplay.LockBits(
                                        screenRect,
                                       System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                //Copy the data from the byte array into BitmapData.Scan0
                lock (ziggyWin.zx)
				{
                    System.Runtime.InteropServices.Marshal.Copy(ziggyWin.zx.ScreenBuffer, 0, bmpData.Scan0, (ScreenWidth) * (ScreenHeight));
                }
                //Unlock the pixels
                gdiDisplay.UnlockBits(bmpData);

                System.IntPtr hdc = e.Graphics.GetHdc();
                System.IntPtr hbmp = gdiDisplay.GetHbitmap();
                System.IntPtr memdc = CreateCompatibleDC(hdc);

                SelectObject(memdc, hbmp);
                StretchBlt(hdc, 0, 0, this.Width, this.Height, memdc, 0, 0, ScreenWidth, ScreenHeight, 0xCC0020);

                e.Graphics.ReleaseHdc(hdc);

                DeleteObject(hbmp);
                DeleteDC(memdc);
            }
            isRendering = false;
        }

        public Bitmap GetScreen()
		{
            Bitmap saveBmp;
			System.Drawing.Imaging.BitmapData bmpData = gdiDisplay.LockBits(screenRect,
											  System.Drawing.Imaging.ImageLockMode.WriteOnly,
											  System.Drawing.Imaging.PixelFormat.Format32bppRgb);

			//Copy the data from the byte array into BitmapData.Scan0
			lock (ziggyWin.zx)
			{
				System.Runtime.InteropServices.Marshal.Copy(ziggyWin.zx.ScreenBuffer, 0, bmpData.Scan0, (ScreenWidth) * (ScreenHeight));
			}
			//Unlock the pixels
			gdiDisplay.UnlockBits(bmpData);
			Graphics g1 = this.CreateGraphics();

			saveBmp = new Bitmap(this.Width, this.Height, g1);
			Graphics g2 = Graphics.FromImage(saveBmp);
			System.IntPtr hdc = g2.GetHdc();
			System.IntPtr hbmp = gdiDisplay.GetHbitmap();
			System.IntPtr memdc = CreateCompatibleDC(hdc);

			SelectObject(memdc, hbmp);
			StretchBlt(hdc, 0, 0, this.Width, this.Height, memdc, 0, 0, ScreenWidth, ScreenHeight, 0xCC0020);

			g2.ReleaseHdc(hdc);

			DeleteObject(hbmp);
			DeleteDC(memdc);
			return saveBmp;
        }
        #endregion DIRECTX_RENDERER
    }
}