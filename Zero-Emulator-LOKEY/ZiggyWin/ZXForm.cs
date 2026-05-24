#define ENABLE_WM_EXCHANGE

using Loggers;
using LOKEY;
using Peripherals;
using Speccy;
using SpeccyCommon;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ZeroWin.Tools;

namespace ZeroWin
{
    public partial class ZXForm : Form
    {
		#if ENABLE_WM_EXCHANGE

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr dwData;
            [MarshalAs(UnmanagedType.I4)]
            public int cbData;
            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr lpData;
        }
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

#if _DEBUG
const string WmCpyDta = "WmCpyDta_d.dll";
#else
        const string WmCpyDta = "WmCpyDta.dll";
#endif

        const int WM_COPYDATA = 0x004A;
        const int WM_USER = 0x8000;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSCOMMAND = 0x0112;
        const int WM_NCLBUTTONDBLCLK = 0x00A3; //double click on a title bar a.k.a. non-client area of the form
        const int WM_MAXIMIZE = 0xF030;

        IntPtr tempHandle;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct EMU_STATE
        {
            public int tstates;
            public ushort PC;
            public ushort SP;
            public ushort BC;
            public ushort DE;
            public ushort HL;
            public ushort AF;
            public ushort _BC;
            public ushort _DE;
            public ushort _HL;
            public ushort _AF;
            public ushort IX;
            public ushort IY;
            public byte I;
            public byte R;
            public byte IM;
        }
#endif
        enum EMULATOR_STATE
        {
            NONE,
            IDLE,
            PAUSED,
            RESET,
            PLAYING_RZX,
            RECORDING_RZX,
            TAPE_INSERTED,
            PLAYING_TAPE,
            DISK_INSERTED,
        }

        private EMULATOR_STATE prevState = EMULATOR_STATE.NONE;
        private EMULATOR_STATE state = EMULATOR_STATE.IDLE;
		//private ZRenderer dxWindow;
		private GDIRenderer dxWindow; //ZXIT
        private Monitor debugger;
        private Options optionWindow;
        private AboutBox1 aboutWindow;
        private ZLibrary library;
        private LoadBinary loadBinaryDialog;
        private Trainer_Wizard trainerWiz;
        private Infoseeker infoseekWiz;
        private SpectrumKeyboard speccyKeyboard;
        private Tools.BASICImporter basicImporter;
        public ZeroConfig config = new ZeroConfig();
        private PrecisionTimer timer = new PrecisionTimer();
        private InputSystem inputSystem = new InputSystem();
		public ZeroLogger logger = new ZeroLogger(Application.StartupPath + "\\TempLog " + System.DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".txt");
        public zx_spectrum zx;

        private MachineModel previousMachine = MachineModel._48k;

        private bool capsLockOn = false;
        private bool shiftIsPressed = false;
        private bool altIsPressed = false;
        private bool ctrlIsPressed = false;

        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCTRL = 0xA2;
        private const int VK_RCTRL = 0xA3;
        private const int VK_PRNTSCRN = 0x2C;
        private const int VK_LEFT = 0x25;
        private const int VK_UP = 0x26;
        private const int VK_RIGHT = 0x27;
        private const int VK_DOWN = 0x28;
        private const int VK_ALT = 0xA4;

        private const string ZX_SPECTRUM_48K = "ZX Spectrum 48k";

        private int frameCount = 0;
        private double lastTime = 0;
        private double frameTime = 0;
        private double totalFrameTime = 0;
        private int averageFPS = 50;
        private bool softResetOnly = false;
        private bool isResizing = false;
        public bool invokeMonitor = false;
        public bool pauseEmulation = false;
        public bool tapeFastLoad = false;
        private Point mouseOrigin;
        private Point mouseMoveDiff;
        private Point mouseOldPos;
        private Point oldWindowPosition = new Point();
        private int oldWindowSize = -1;
        public String recentFolder = ".";
        private String ZeroSessionSnapshotName = "_z0_session.szx";
        private string cpuSpeed = "3.5 MHz";

        public bool showDownloadIndicator = false;
        private int downloadIndicatorTimeout = 0;

        private bool romLoaded = false;
        private string[] diskArchivePath = { null, null, null, null };    //Any temp disk file created by the archiver from a .zip file
        private int borderAdjust = 0;

        //The grayscale version of spectrum pallette
        protected int[] GrayPalette =
		{
			0x000000,            // Black
			0x171717,            // Red
			0x3C3C3C,            // Blue
			0x474747,            // Magenta
			0x777777,            // Green
			0xB3B3B3,            // Yellow
			0x8E8E8E,            // Cyan
			0xC6C6C6,            // White
			0x000000,            // Bright Black
			0x1D1D1D,            // Bright Red
			0x4C4C4C,            // Bright Blue
			0x696969,            // Bright Magenta
			0x969696,            // Bright Green
			0xE2E2E2,            // Bright Yellow
			0xB3B3B3,            // Bright Cyan
			0xffffff             // Bright White
		};

        private const int SV_LAST_K = 23560; //last pressed key
        private const int SV_FLAGS = 23611;  //bit 5 of FLAGS is set to indicate keypress
        private bool commandLineLaunch = false;
        private bool doAutoLoadTape = false;
        private int autoTapeLoadCounter = 0;
        private string autoLoadFile = null;
        private byte[] AutoLoadTape48Keys = { 239, 34, 34, 13 }; //LOAD "" + Enter

        //Command queue.
        //If there are commands in the queue, Zero will execute them one after the other
        private Queue<string> commandBuffer = new Queue<string>();
        private bool isProcessingCommands = false;
        private int speccyFrameCount = 0;
        private int numSpeccyFramesToWait = 0;
        private StreamWriter traceFile = null;
        private bool isTracing = false;

        //For the +3, we have to type in individual keys, which makes it a bit more involved:
        //Cursor DOWN + ENTER (to enter BASIC) then this string: load "t:":load "" + Enter
        private byte[] AutoLoadTapePlus3Keys = { 10, 13, 108, 111, 97, 100, 32, 34, 116, 58, 34, 58, 108, 111, 97, 100, 32, 34, 34, 13 };

        public TimeSpan TimeoutToHide { get; private set; }

        public DateTime LastMouseMove { get; private set; }

        public bool CursorIsHidden { get; private set; }

#if ENABLE_WM_EXCHANGE
        unsafe protected override void WndProc(ref Message message)
		{
            if (message.Msg == WM_SYSCOMMAND) {
                if (message.WParam == new IntPtr(WM_MAXIMIZE)) {
                    GoFullscreen(true);
                    return;
                }
                else if (message.WParam == new IntPtr(0xF120)) //Restore?
                {
                    GoFullscreen(false);
                    return;
                }
            }
            else if (message.Msg == WM_NCLBUTTONDBLCLK) {
                GoFullscreen(true);
                return;
            }
            else if (message.Msg == WM_COPYDATA) {
                COPYDATASTRUCT data = (COPYDATASTRUCT)
                message.GetLParam(typeof(COPYDATASTRUCT));

                byte[] b = System.BitConverter.GetBytes((int)(data.dwData));
                string str = String.Format("{3}{2}{1}{0}", (char)b[0], (char)b[1], (char)b[2], (char)b[3]);

                if (str == "PAUS") {
                    if (!pauseEmulation)
                        PauseEmulation(true);

                    SendWMCOPYDATA("SUAP", message.WParam, (IntPtr)0, 0);
                }
                else if (str == "SNAP") {
                    string snapFile = Marshal.PtrToStringAnsi(data.lpData);
                    LoadZXFile(snapFile);
                    SendWMCOPYDATA("PANS", message.WParam, (IntPtr)0, 0);
                }
                else if (str == "STEP") {
                    tempHandle = message.WParam;

                    PostMessage(this.Handle, WM_USER + 2, message.WParam, data.dwData);
                }
            }
            else if (message.Msg == WM_USER + 2)
			{
                zx.externalSingleStep = true;
                zx.Run();
                zx.UpdateScreenBuffer(zx.FrameLength);
                ForceScreenUpdate();
                EMU_STATE emuState = new EMU_STATE();
                emuState.tstates = zx.cpu.t_states;
                emuState.PC = zx.cpu.regs.PC;
                emuState.SP = zx.cpu.regs.SP;
                emuState.IX = zx.cpu.regs.IX;
                emuState.IY = zx.cpu.regs.IY;
                emuState.HL = zx.cpu.regs.HL;
                emuState.DE = zx.cpu.regs.DE;
                emuState.BC = zx.cpu.regs.BC;
                emuState.AF = zx.cpu.regs.AF;
                emuState._HL = zx.cpu.regs.HL_;
                emuState._DE = zx.cpu.regs.DE_;
                emuState._BC = zx.cpu.regs.BC_;
                emuState._AF = zx.cpu.regs.AF_;
                emuState.I = zx.cpu.regs.I;
                emuState.R = (byte)((zx.cpu.regs.R & 0x7f) | (zx.cpu.regs.R_ & 0x80));
                emuState.IM = zx.cpu.interrupt_mode;

                IntPtr lpStruct = Marshal.AllocHGlobal(
                    Marshal.SizeOf(emuState));

                Marshal.StructureToPtr(emuState, lpStruct, false);

                int emuStatSize = Marshal.SizeOf(emuState);
                SendWMCOPYDATA("PETS", tempHandle, lpStruct, emuStatSize);
                Marshal.FreeHGlobal(lpStruct);
                zx.externalSingleStep = false;
            }
            base.WndProc(ref message);
        }

        unsafe private void SendWMCOPYDATA(String s, IntPtr _hTarget, IntPtr _lpData, int _size) {
            byte[] carray = System.Text.ASCIIEncoding.UTF8.GetBytes(s);
            uint val = BitConverter.ToUInt32(carray, 0);

            IntPtr dwData = (IntPtr)val;

            COPYDATASTRUCT data = new COPYDATASTRUCT();
            data.dwData = dwData;
            data.cbData = _size;
            data.lpData = _lpData;

            IntPtr lpStruct = Marshal.AllocHGlobal(
                Marshal.SizeOf(data));

            Marshal.StructureToPtr(data, lpStruct, false);

            SendMessage(_hTarget, WM_COPYDATA, this.Handle, lpStruct);
            Marshal.FreeHGlobal(lpStruct);
        }
#endif
        private void CloseInfoseekWiz(object sender, EventArgs e) {
            ((System.Timers.Timer)sender).Enabled = false;
            infoseekWiz.Hide();
            LoadZXFile(autoLoadFile);
        }

        public void OnFileDownloadEvent(Object sender, AutoLoadArgs arg) {
            if (!string.IsNullOrEmpty(arg.filePath)) {
                if (MessageBox.Show("You've selected a file to auto-load on completion of download. Auto-load now?", "Auto Load", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK) {
                    autoLoadFile = arg.filePath;

                    //  DispatcherTimer setup

                    System.Timers.Timer dispatcherTimer = new System.Timers.Timer();
                    dispatcherTimer.Elapsed += new System.Timers.ElapsedEventHandler(CloseInfoseekWiz);
                    dispatcherTimer.Interval = 500;
                    dispatcherTimer.Enabled = true;
                    dispatcherTimer.SynchronizingObject = infoseekWiz;
                    dispatcherTimer.Start();
                }
            }
            else {
                showDownloadIndicator = true;
                downloadIndicatorTimeout = 5000;
            }
        }

        public ZXForm()
		{
            InitializeComponent();
            toolTip1.Active = false;
            toolTip1.UseAnimation = false;
            toolTip1.UseFading = false;
            toolTip1.ShowAlways = false;
			//seems to stop the stutter when tooltip appears in fullscreen mode...
            toolTip1.IsBalloon = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseMove);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Opaque, true);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            TimeoutToHide = TimeSpan.FromSeconds(5);
            panel1.SendToBack();
		}

        private void EjectD_Click(object sender, EventArgs e) {
            zx.DiskEject(3);
            insertDiskDToolStripMenuItem.Text = "D: -- Empty --";
            insertDiskDToolStripMenuItem.DropDownItems.Clear();
        }

        private void EjectC_Click(object sender, EventArgs e) {
            zx.DiskEject(2);
            insertDiskCToolStripMenuItem.Text = "C: -- Empty --";
            insertDiskCToolStripMenuItem.DropDownItems.Clear();
        }

        private void EjectB_Click(object sender, EventArgs e) {
            zx.DiskEject(1);
            insertDiskBToolStripMenuItem.Text = "B: -- Empty --";
            insertDiskBToolStripMenuItem.DropDownItems.Clear();
        }

        private void EjectA_Click(object sender, EventArgs e) {
            zx.DiskEject(0);
            insertDiskAToolStripMenuItem.Text = "A: -- Empty --";
            insertDiskAToolStripMenuItem.DropDownItems.Clear();
        }

        protected override void OnActivated(EventArgs e)
		{
            //pauseEmulation = false;
            base.OnActivated(e);
        }

        protected override void OnDeactivate(EventArgs e)
		{
            if (config.emulationOptions.PauseOnFocusLost)
                if (!(AppHasFocus()))
				{
                    // pauseEmulation = true;
                }
            base.OnDeactivate(e);
        }

        public static class Native
        {
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct Message
            {
                public IntPtr hWnd;

                [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
                public int msg;

                public IntPtr wParam;
                public IntPtr lParam;

                [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
                public uint time;

                public System.Drawing.Point p;
            }

            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            [System.Security.SuppressUnmanagedCodeSecurity, System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
            public static extern bool PeekMessage(out Message msg, IntPtr hWnd,
                [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
                uint messageFilterMin,
                [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
                uint messageFilterMax,
                [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.U4)]
                uint flags);

            [System.Runtime.InteropServices.DllImport("User32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, ExactSpelling = true)]
            public static extern short GetAsyncKeyState([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I4)] int vkey);
        }

        private bool AppStillIdle
        {
            get
            {
                Native.Message msg;
                return !Native.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        public bool AppHasFocus()
		{
            if ((Native.GetForegroundWindow() == this.Handle))
                return true;
            else
                if ((debugger != null) && (!debugger.IsDisposed))
                if (Native.GetForegroundWindow() == debugger.Handle)
                    return true;
            return false;
        }

        public void ForceScreenUpdate(bool doFullScreen = false)
		{
			if (doFullScreen)
			{
				zx.UpdateScreenBuffer(zx.FrameLength);
			}   
            zx.needsPaint = true;
            System.Threading.Thread.Sleep(1);
        }

        private void SetEmulationState(EMULATOR_STATE newState)
		{
            if (newState == state)
                return;

            prevState = state;
            state = newState;

            switch (state)
			{

                case EMULATOR_STATE.IDLE:
                statusLabelText.Text = "Ready";
                zx.Resume();
                break;

                case EMULATOR_STATE.PAUSED:
                statusLabelText.Text = "Emulation Paused";
                zx.Pause();
                break;

                case EMULATOR_STATE.TAPE_INSERTED:
                statusLabelText.Text = "Tape inserted: " + zx.tapeFilename;
                break;

                case EMULATOR_STATE.PLAYING_TAPE: {
                        string[] s = zx.tapeFilename.Split('\\');
                        statusLabelText.Text = "Playing tape: " + s[s.Length - 1];
                        break;
                }
            }

        }

        private int GetSpectrumModelIndex(string speccyModel)
		{
            int modelIndex = 0;
            switch (speccyModel)
			{
                case ZX_SPECTRUM_48K:
                modelIndex = 0;
                break;
            }
            return modelIndex;
        }

        private void HandleKey2Joy(int key, bool pressed)
		{
            if (pressed) {
                switch (key) {
                    case ((int)keyCode.RIGHT):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] |= SpeccyGlobals.JOYSTICK_MOVE_RIGHT;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._2] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._7] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._8] = true;
                    break;

                    case ((int)keyCode.LEFT):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] |= SpeccyGlobals.JOYSTICK_MOVE_LEFT;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._1] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._6] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._5] = true;
                    break;

                    case ((int)keyCode.DOWN):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] |= SpeccyGlobals.JOYSTICK_MOVE_DOWN;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._3] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._8] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._6] = true;
                    break;

                    case ((int)keyCode.UP):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] |= SpeccyGlobals.JOYSTICK_MOVE_UP;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._4] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._9] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._7] = true;
                    break;

                    case (255): //proxy for Fire
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] |= SpeccyGlobals.JOYSTICK_BUTTON_1;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._5] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._0] = true;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._0] = true;
                    break;
                }
            }
            else {
                switch (key) {
                    case ((int)keyCode.RIGHT):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] &= ~((int)(SpeccyGlobals.JOYSTICK_MOVE_RIGHT));
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._2] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._7] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._8] = false;
                    break;

                    case ((int)keyCode.LEFT):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] &= ~((int)(SpeccyGlobals.JOYSTICK_MOVE_LEFT));
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._1] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._6] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._5] = false;
                    break;

                    case ((int)keyCode.DOWN):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] &= ~((int)(SpeccyGlobals.JOYSTICK_MOVE_DOWN));
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._3] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._8] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._6] = false;
                    break;

                    case ((int)keyCode.UP):
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] &= ~((int)(SpeccyGlobals.JOYSTICK_MOVE_UP));
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._4] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._9] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._7] = false;
                    break;

                    case (255): //proxy for alt
                    if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON)
                        zx.joystickState[config.inputDeviceOptions.Key2JoystickType] &= ~((int)(SpeccyGlobals.JOYSTICK_BUTTON_1));
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR2)
                        zx.keyBuffer[(int)keyCode._5] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.SINCLAIR1)
                        zx.keyBuffer[(int)keyCode._0] = false;
                    else if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.CURSOR)
                        zx.keyBuffer[(int)keyCode._0] = false;
                    break;
                }
            }
            if (config.inputDeviceOptions.Key2JoystickType == (int)zx_spectrum.JoysticksEmulated.KEMPSTON) {
                inputSystem.kempstonJoystick.SetState((byte)zx.joystickState[config.inputDeviceOptions.Key2JoystickType]);
            }
        }

        private void AutoTapeLoad()
		{
            if (zx.model == MachineModel._48k)
			{
                if ((zx.PeekByteNoContend(SV_FLAGS) & 0x20) == 0) {
                    zx.PokeByteNoContend(SV_LAST_K, AutoLoadTape48Keys[autoTapeLoadCounter]);
                    zx.PokeByteNoContend(SV_FLAGS, zx.PeekByteNoContend(SV_FLAGS) | 0x20);
                    autoTapeLoadCounter++;
                    if (autoTapeLoadCounter >= AutoLoadTape48Keys.Length) {
                        autoTapeLoadCounter = 0;
                        doAutoLoadTape = false;
                    }
                }
            }
            else
			{
                if ((zx.PeekByteNoContend(SV_FLAGS) & 0x20) == 0) {
                    zx.PokeByteNoContend(SV_LAST_K, 13);
                    zx.PokeByteNoContend(SV_FLAGS, zx.PeekByteNoContend(SV_FLAGS) | 0x20);
                    doAutoLoadTape = false;
                }
            }
        }

        public void AddKeywordToEditorBuffer(byte token)
		{
            zx.PokeByteNoContend(SV_LAST_K, token);
            zx.PokeByteNoContend(SV_FLAGS, zx.PeekByteNoContend(SV_FLAGS) | 0x20);
        }

        public void OnSpeccyFrameEnd(object sender)
		{
            speccyFrameCount += 1;
            frameCount += 1;
            if ((numSpeccyFramesToWait > 0) && (speccyFrameCount >= numSpeccyFramesToWait))
			{
                numSpeccyFramesToWait = 0;
                isProcessingCommands = false;
                logger.Log("Completed waitframes.");
                ProcessNextCommand();
            }
		}

        public void OnSpeccyExecutedOpcode(object sender)
		{
            string s = String.Format("${0, 4:x4}\t{1, 5}\t{2}", zx.cpu.regs.PC, zx.cpu.t_states, zx.Disassemble(zx.cpu.regs.PC));
            traceFile.WriteLine(s);
        }

        private void DumpSpeccyRegisters() {
            String s = String.Format("PC: ${0, 4:x4}    SP: ${1, 4:x4}", zx.cpu.regs.PC, zx.cpu.regs.SP);
            traceFile.WriteLine(s);
            s = String.Format("IX: ${0, 4:x4}    IY: ${1, 4:x4}", zx.cpu.regs.IX, zx.cpu.regs.IY);
            traceFile.WriteLine(s);
            s = String.Format("HL: ${0, 4:x4}    HL': ${1, 4:x4}", zx.cpu.regs.HL, zx.cpu.regs.HL_);
            traceFile.WriteLine(s);
            s = String.Format("DE: ${0, 4:x4}    DE': ${1, 4:x4}", zx.cpu.regs.DE, zx.cpu.regs.DE_);
            traceFile.WriteLine(s);
            s = String.Format("BC: ${0, 4:x4}    BC': ${1, 4:x4}", zx.cpu.regs.BC, zx.cpu.regs.BC_);
            traceFile.WriteLine(s);
            s = String.Format("AF: ${0, 4:x4}    AF': ${1, 4:x4}", zx.cpu.regs.AF, zx.cpu.regs.AF_);
            traceFile.WriteLine(s);
            traceFile.WriteLine("");
        }

        public void ProcessNextCommand()
		{
            if (isProcessingCommands)
                return;

            if (commandBuffer.Count > 0) {
                string c = commandBuffer.Dequeue();
                switch (c) {
                    case "/waitframes": {
                            //zx.FrameEndEvent += OnSpeccyFrameEnd;
                            speccyFrameCount = 0;
                            numSpeccyFramesToWait = Convert.ToInt32(commandBuffer.Dequeue());
                            if (numSpeccyFramesToWait > 0) {
                                logger.Log(String.Format("Waiting for {0} frames.", numSpeccyFramesToWait));
                                isProcessingCommands = true;
                            }
                            break;
                        }
                    case "/loadfile": {
                            string filename = commandBuffer.Dequeue();
                            logger.Log(String.Format("Loading file.", filename));
                            LoadZXFile(filename);
                            isProcessingCommands = false;
                            break;
                        }
                    case "/trace": {
                            string filename = commandBuffer.Dequeue();
                            logger.Log(String.Format("Opening trace file {0}", filename));
                            try {
                                traceFile = File.CreateText(filename);
                                zx.OpcodeExecutedEvent += OnSpeccyExecutedOpcode;
                                DumpSpeccyRegisters();
                                isTracing = true;
                            }
                            catch {
                                MessageBox.Show("Failed to open file " + filename + " for trace.", "Command failure", MessageBoxButtons.OK);
                                commandBuffer.Clear();
                            }
                            isProcessingCommands = false;
                            break;
                        }
                    case "/savesnap":
						{
                            string filename = commandBuffer.Dequeue();
                            logger.Log("Saving snapshot.");
                            isProcessingCommands = false;
                            break;
                        }
                    case "/debug": {
                            monitorButton_Click(this, null);
                            break;
                        }
                    case "/stoptrace": {
                            logger.Log("Stopping trace.");
                            zx.OpcodeExecutedEvent -= OnSpeccyExecutedOpcode;
                            isProcessingCommands = false;
                            try {
                                traceFile.WriteLine("");
                                DumpSpeccyRegisters();
                                traceFile.Flush();
                                traceFile.Close();
                                isTracing = false;
                            }
                            catch {
                                MessageBox.Show("Failed to close file for trace.", "Command failure", MessageBoxButtons.OK);
                                commandBuffer.Clear();
                            }
                            break;
                        }

                    case "/exit": {
                            logger.Log("Exiting.");
                            commandBuffer.Clear();
                            config.emulationOptions.ConfirmOnExit = false;
                            this.Close();
                            break;
                        }
                }
            }
            else {
                isProcessingCommands = false;
            }
        }

        public void OnApplicationIdle(object sender, EventArgs e)
		{
            while (AppStillIdle && !pauseEmulation)
			{
                while (!isProcessingCommands && commandBuffer.Count > 0)
				{
                    ProcessNextCommand();
                    if (isProcessingCommands)
					{
                        break;
                    }
                }
                TimeSpan elapsed = DateTime.Now - LastMouseMove;

				if (zx.doRun) { zx.Run(); }

                frameTime = PrecisionTimer.TimeInMilliseconds();
                totalFrameTime += frameTime - lastTime;
                lastTime = frameTime;
                //Start the auto load process only if we aren't in the middle of a reset
                if (zx.isResetOver)
				{
					if (doAutoLoadTape) { AutoTapeLoad(); }
                }

                inputSystem.UpdateInputs();

                if (config.audioOptions.Mute) //we'll try and synch to ~60Hz framerate (50Hz makes it run slightly slower than audio synch)
                {
                    if ((frameTime) < 19 && !((zx.tapeIsPlaying && tapeFastLoad)))
					{
                        double sleepTime = ((19 - frameTime));
                        System.Threading.Thread.Sleep((int)sleepTime);
                    }
                }

                if (showDownloadIndicator)
				{
                    downloadIndicatorTimeout--;

                    if (downloadIndicatorTimeout <= 0) {
                        fileDownloadStatusLabel.Enabled = false;
                        showDownloadIndicator = false;
                    }
                    else
                        fileDownloadStatusLabel.Enabled = true;
                }

                if (frameCount >= 50)
				{
                    averageFPS = (int)(1000 / (totalFrameTime / frameCount));
     //               if (!dxWindow.EnableFullScreen)
					//{
     //                   fpsStatusLabel.Text = Math.Max(0, averageFPS).ToString() + " FPS ";
     //               }
					fpsStatusLabel.Text = Math.Max(0, averageFPS).ToString() + " FPS ";//ZXIT
					frameCount = 0;
                    totalFrameTime = 0;
                    averageFPS = 0;
                }
                System.Threading.Thread.Sleep(1);
                dxWindow.Invalidate();
			}
		}

        public void EnableMouse(bool isEnabled)
		{
            if (!isEnabled)
			{
                inputSystem.ReleaseMouse();
                mouseStripStatusLabel.Enabled = false;
            }
            else if (isEnabled)
			{
                if (config.inputDeviceOptions.EnableKempstonMouse) {
                    inputSystem.EnableMouse();
                    mouseStripStatusLabel.Enabled = true;
                    statusLabelText.Text = "Press F6 to release mouse.";
                }
            }
        }

        public string GetConfigData(StreamReader sr, string section, string data)
		{
            String readStr = "dummy";

            while (readStr != section) {
                if (sr.EndOfStream == true) {
                    System.Windows.Forms.MessageBox.Show("Invalid config file!", "Config file error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    return "error";
                }
                readStr = sr.ReadLine();
            }

            while (true) {
                readStr = sr.ReadLine();
                if (readStr.IndexOf(data) >= 0)
                    break;
                if (sr.EndOfStream == true) {
                    System.Windows.Forms.MessageBox.Show("Invalid config file!", "Config file error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    return "error";
                }
            }

            int startIndex = readStr.IndexOf("=") + 1;
            String dataString = readStr.Substring(startIndex, readStr.Length - startIndex);

            return dataString;
        }

        private bool LoadROM(String romName)
		{
            logger.Log("Booting ROM: " + romName);

            byte[] romData;
            romLoaded = Utilities.ReadBytesFromFile(config.pathOptions.Roms + "\\" + romName, out romData);

            //Next try the application startup path (useful if running off USB)
            if (!romLoaded)
			{
                romLoaded = Utilities.ReadBytesFromFile(Application.StartupPath + "\\roms\\" + romName, out romData);

                //Aha! This worked so update the path in config file
                if (romLoaded)
                    config.pathOptions.Roms = Application.StartupPath + "\\roms";
            }

            while (!romLoaded)
			{
                MessageBox.Show("Zero couldn't load the ROM file for the " +
                                Utilities.GetStringFromEnum(zx.model) + ".\nSelect a valid ROM to continue.", "Missing ROM",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                openFileDialog1.InitialDirectory = config.pathOptions.Roms;
                openFileDialog1.Title = "Choose a ROM";
                openFileDialog1.FileName = "";
                openFileDialog1.Filter = "All supported files|*.rom;";

                if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                    romName = openFileDialog1.SafeFileName;
                    config.pathOptions.Roms = Path.GetDirectoryName(openFileDialog1.FileName);
                    romLoaded = Utilities.ReadBytesFromFile(config.pathOptions.Roms + "\\" + romName, out romData);
                }
                else {
                    MessageBox.Show("Unfortunately, Zero cannot work without a valid ROM file.\nIt will now exit.",
                            "Unable to continue!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    break;
                }
            }

            if (romLoaded)
			{
                switch (zx.model) {
                    case MachineModel._48k:
						if (romData.Length != 16384)
						{
							romLoaded = false;
						}
						else
						{
							config.romOptions.Current48kROM = romName;
							PatchRom(romData);
							zx.PokeROMPages(0, 16384, romData);
						}
						break;
                }
            }
            return romLoaded;
        }

		private void PatchRom(byte[] romData)
		{
			//PatchRAMTOP(romData);
			//PatchPlotHook(romData);
			//InstallPlotTrampoline();
		}
		public void PatchRAMTOP(byte[] romData)
		{
			// ROM startup contains:
			// LD HL,FFFF
			// LD (RAMTOP),HL

			const ushort newRamTop = 0xF000;

			// Find known ROM location (48K ROM fixed offset)
			// 0x11CC is low/high byte immediate operand of FFFF in initialization sequence

			romData[0x11CC] = (byte)(newRamTop & 0xFF);       // low byte = 00
			romData[0x11CD] = (byte)(newRamTop >> 8);         // high byte = F0
		}
		public void PatchPlotHook(byte[] romData)
		{
			const ushort plotEntry = 0x22DC;
			const ushort trampoline = 0xF000;

			romData[plotEntry + 0] = 0xC3; // JP nn
			romData[plotEntry + 1] = (byte)(trampoline & 0xFF);
			romData[plotEntry + 2] = (byte)(trampoline >> 8);
		}
		public void InstallPlotTrampoline()
		{
			const ushort addr = 0xF000;

			const byte DATAPORT = 129;
			const byte CMDPORT = 131;
			const byte PLOT_CMD = 20;

			int p = addr;

			// LD A,C  ; X
			zx.PokeByteNoContend(p++, 0x79);

			// OUT (129),A
			zx.PokeByteNoContend(p++, 0xD3);
			zx.PokeByteNoContend(p++, DATAPORT);

			// LD A,B  ; Y
			zx.PokeByteNoContend(p++, 0x78);

			// OUT (129),A
			zx.PokeByteNoContend(p++, 0xD3);
			zx.PokeByteNoContend(p++, DATAPORT);

			// LD A,20
			zx.PokeByteNoContend(p++, 0x3E);
			zx.PokeByteNoContend(p++, PLOT_CMD);

			// OUT (131),A
			zx.PokeByteNoContend(p++, 0xD3);
			zx.PokeByteNoContend(p++, CMDPORT);

			// RET
			zx.PokeByteNoContend(p++, 0xC9);
		}

		private bool OldLoadROM(String romName)
		{
            //First try to load from the path saved in the config file
            romLoaded = zx.LoadROM(config.pathOptions.Roms + "\\", romName);
            logger.Log("Booting ROM: " + romName);
            //Next try the application startup path (useful if running off USB)
            if (!romLoaded) {
                romLoaded = zx.LoadROM(Application.StartupPath + "\\roms\\", romName);

                //Aha! This worked so update the path in config file
                if (romLoaded)
                    config.pathOptions.Roms = Application.StartupPath + "\\roms";
            }
            while (!romLoaded) {
                MessageBox.Show("Zero couldn't find the '" + romName + "' file for the " +
                                Utilities.GetStringFromEnum(zx.model) + ".\nSelect a valid ROM to continue.", "Missing ROM",
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                openFileDialog1.InitialDirectory = config.pathOptions.Roms;
                openFileDialog1.Title = "Choose a ROM";
                openFileDialog1.FileName = "";
                openFileDialog1.Filter = "All supported files|*.rom;";

                if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                    romName = openFileDialog1.SafeFileName;
                    
                    config.pathOptions.Roms = Path.GetDirectoryName(openFileDialog1.FileName);
                    romLoaded = zx.LoadROM(config.pathOptions.Roms + "\\", romName);

                    if (romLoaded)
					{
                        switch (zx.model)
						{
                            case MachineModel._48k:
                            config.romOptions.Current48kROM = openFileDialog1.SafeFileName;
                            break;
                        }
                    }
                }
                else {
                    MessageBox.Show("Unfortunately, Zero cannot work without a valid ROM file.\nIt will now exit.",
                            "Unable to continue!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    break;
                }
            }

            return romLoaded;
        }

        protected override void OnKeyDown(KeyEventArgs keyEvent)
		{
            if (menuStrip1.Focused) {
                return;
            }
            shiftIsPressed = (((Native.GetAsyncKeyState(VK_LSHIFT) & 0x8000) | (Native.GetAsyncKeyState(VK_RSHIFT) & 0x8000)) != 0); //|| ((Native.GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0);// (keyEvent.KeyCode & Keys.Shift) != 0;
            ctrlIsPressed = (((keyEvent.Modifiers & Keys.Control) != 0)); //((Native.GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0) ||
            altIsPressed = (keyEvent.Modifiers & Keys.Alt) != 0;

            zx.keyBuffer[(int)keyCode.SHIFT] = shiftIsPressed;
            zx.keyBuffer[(int)keyCode.CTRL] = ctrlIsPressed;
            zx.keyBuffer[(int)keyCode.ALT] = altIsPressed;

            switch (keyEvent.KeyCode) {
                case Keys.A:
                zx.keyBuffer[(int)keyCode.A] = true;
                break;

                case Keys.B:
                zx.keyBuffer[(int)keyCode.B] = true;
                break;

                case Keys.C:
                zx.keyBuffer[(int)keyCode.C] = true;
                break;

                case Keys.D:
                zx.keyBuffer[(int)keyCode.D] = true;
                break;

                case Keys.E:
                zx.keyBuffer[(int)keyCode.E] = true;
                break;

                case Keys.F:
                zx.keyBuffer[(int)keyCode.F] = true;
                break;

                case Keys.G:
                zx.keyBuffer[(int)keyCode.G] = true;
                break;

                case Keys.H:
                zx.keyBuffer[(int)keyCode.H] = true;
                break;

                case Keys.I:
                zx.keyBuffer[(int)keyCode.I] = true;
                break;

                case Keys.J:
                zx.keyBuffer[(int)keyCode.J] = true;
                break;

                case Keys.K:
                zx.keyBuffer[(int)keyCode.K] = true;
                break;

                case Keys.L:
                zx.keyBuffer[(int)keyCode.L] = true;
                break;

                case Keys.M:
                zx.keyBuffer[(int)keyCode.M] = true;
                break;

                case Keys.N:
                zx.keyBuffer[(int)keyCode.N] = true;
                break;

                case Keys.O:
                if (ctrlIsPressed) {
                    fileButton_Click(this, null);
                }
                else
                    zx.keyBuffer[(int)keyCode.O] = true;
                break;

                case Keys.P:
                zx.keyBuffer[(int)keyCode.P] = true;
                break;

                case Keys.Q:
                zx.keyBuffer[(int)keyCode.Q] = true;
                break;

                case Keys.R:
                zx.keyBuffer[(int)keyCode.R] = true;
                break;

                case Keys.S:
                if (ctrlIsPressed) {
                    saveSnapshotMenuItem_Click(this, null);
                }
                else
                    zx.keyBuffer[(int)keyCode.S] = true;
                break;

                case Keys.T:
                if (ctrlIsPressed)
				{
                }
                else
                    zx.keyBuffer[(int)keyCode.T] = true;
                break;

                case Keys.U:
                zx.keyBuffer[(int)keyCode.U] = true;
                break;

                case Keys.V:
                zx.keyBuffer[(int)keyCode.V] = true;
                break;

                case Keys.W:
                zx.keyBuffer[(int)keyCode.W] = true;
                break;

                case Keys.X:
                zx.keyBuffer[(int)keyCode.X] = true;
                break;

                case Keys.Y:
                zx.keyBuffer[(int)keyCode.Y] = true;
                break;

                case Keys.Z:
                zx.keyBuffer[(int)keyCode.Z] = true;
                break;

                case Keys.D0:
                if (altIsPressed)
                    size100ToolStripMenuItem_Click(this, null);
                else
                    zx.keyBuffer[(int)keyCode._0] = true;
                break;

                case Keys.D1:
                zx.keyBuffer[(int)keyCode._1] = true;
                break;

                case Keys.D2:
                zx.keyBuffer[(int)keyCode._2] = true;
                break;

                case Keys.D3:
                zx.keyBuffer[(int)keyCode._3] = true;
                break;

                case Keys.D4:
                zx.keyBuffer[(int)keyCode._4] = true;
                break;

                case Keys.D5:
                zx.keyBuffer[(int)keyCode._5] = true;
                break;

                case Keys.D6:
                zx.keyBuffer[(int)keyCode._6] = true;
                break;

                case Keys.D7:
                zx.keyBuffer[(int)keyCode._7] = true;
                break;

                case Keys.D8:
                zx.keyBuffer[(int)keyCode._8] = true;
                break;

                case Keys.D9:
                zx.keyBuffer[(int)keyCode._9] = true;
                break;

                case Keys.Enter:
                if (altIsPressed)
                    fullScreenToolStripMenuItem_Click(this, null);
                else
                    zx.keyBuffer[(int)keyCode.ENTER] = true;
                break;

                case Keys.Space:
                if (!altIsPressed)
                    zx.keyBuffer[(int)keyCode.SPACE] = true;
                break;

                case Keys.PrintScreen:
                //screenshotMenuItem1_Click(this, null);
                break;

                case Keys.Up:
                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.UP, true);

                zx.keyBuffer[(int)keyCode.UP] = true;
                break;

                case Keys.Left:
                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.LEFT, true);

                zx.keyBuffer[(int)keyCode.LEFT] = true;
                break;

                case Keys.Right:
                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.RIGHT, true);

                zx.keyBuffer[(int)keyCode.RIGHT] = true;
                break;

                case Keys.Down:
                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.DOWN, true);

                zx.keyBuffer[(int)keyCode.DOWN] = true;
                break;

                case Keys.Back:
                zx.keyBuffer[(int)keyCode.BACK] = true;
                break;

                case Keys.Tab:
                zx.keyBuffer[(int)keyCode.TAB] = true;
                break;

                #region Convenience Key Press Emulation

                case Keys.OemPeriod:
                if (ctrlIsPressed) {
                    zx.keyBuffer[(int)keyCode.T] = true;
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode.M] = true;
                }
                break;

                case Keys.Oemcomma:
                if (ctrlIsPressed) {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode.R] = true;
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode.N] = true;
                }
                break;

                case Keys.OemSemicolon:
                if (ctrlIsPressed) //colon
                {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode.Z] = true;
                    zx.keyBuffer[(int)keyCode.SHIFT] = false; //confuses speccy otherwise!
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode.O] = true;
                }
                break;

                case Keys.OemQuotes:
                if (ctrlIsPressed) //double quotes
                {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode.P] = true;
                    zx.keyBuffer[(int)keyCode.SHIFT] = false; //confuses speccy otherwise!
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.keyBuffer[(int)keyCode._7] = true;
                    zx.keyBuffer[(int)keyCode.SHIFT] = false; //confuses speccy otherwise!
                }
                break;

                case Keys.Oem4: //brace open
                if (ctrlIsPressed) {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.PokeByteNoContend(23617, 1);
                    zx.keyBuffer[(int)keyCode.F] = true;
                    zx.keyBuffer[(int)keyCode.Y] = false;
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.PokeByteNoContend(23617, 1);
                    zx.keyBuffer[(int)keyCode.Y] = true;
                    zx.keyBuffer[(int)keyCode.F] = false;
                }
                break;

                case Keys.Oem6: //brace close
                if (ctrlIsPressed) {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.PokeByteNoContend(23617, 1);
                    zx.keyBuffer[(int)keyCode.G] = true;
                    zx.keyBuffer[(int)keyCode.U] = false;
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.PokeByteNoContend(23617, 1);
                    zx.keyBuffer[(int)keyCode.U] = true;
                    zx.keyBuffer[(int)keyCode.G] = false;
                }
                break;

                case Keys.OemMinus:
                if (altIsPressed)
                    toolStripMenuItem1_Click(this, null);
                else {
                    if (ctrlIsPressed) {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                        zx.keyBuffer[(int)keyCode._0] = true;
                    }
                    else {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                        zx.keyBuffer[(int)keyCode.J] = true;
                    }
                }
                break;

                case Keys.Oemplus:
                if (altIsPressed)
                    toolStripMenuItem5_Click_1(this, null);
                else {
                    if (ctrlIsPressed) {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                        zx.keyBuffer[(int)keyCode.K] = true;
                    }
                    else {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                        zx.keyBuffer[(int)keyCode.L] = true;
                    }
                }
                break;

                case Keys.OemPipe:
                if (ctrlIsPressed) {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.PokeByteNoContend(23617, 1);
                    zx.keyBuffer[(int)keyCode.S] = true;
                    zx.keyBuffer[(int)keyCode.D] = false;
                }
                else {
                    zx.keyBuffer[(int)keyCode.CTRL] = true;
                    zx.PokeByteNoContend(23617, 1);
                    zx.keyBuffer[(int)keyCode.D] = true;
                    zx.keyBuffer[(int)keyCode.S] = false;
                }
                break;

                case Keys.Oemtilde:
                zx.keyBuffer[(int)keyCode.CTRL] = true;
                zx.PokeByteNoContend(23617, 1);
                zx.keyBuffer[(int)keyCode.A] = true;
                break;

                #endregion Convenience Key Press Emulation

                case Keys.F6:
                EnableMouse(!mouseStripStatusLabel.Enabled);

                break;

                case Keys.Insert:
                break;

                case Keys.Delete:
                break;
                case Keys.F7:
                break;
                case Keys.Escape:
                pauseEmulationESCToolStripMenuItem_Click(this, null);
                break;

                case Keys.ShiftKey:
                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy(255, true);
                break;

                case Keys.ControlKey: {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                    }
                    break;

                default:
                if (keyEvent.KeyValue == 191) //frontslash
                {
                    if (ctrlIsPressed) //question mark
                    {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                        zx.keyBuffer[(int)keyCode.C] = true;
                        zx.keyBuffer[(int)keyCode.SHIFT] = false; //confuses speccy otherwise!
                    }
                    else {
                        zx.keyBuffer[(int)keyCode.CTRL] = true;
                        zx.keyBuffer[(int)keyCode.V] = true;
                    }
                }
                break;
            }
        }

        protected override void OnKeyUp(KeyEventArgs keyEvent)
		{
            // if (ctrlIsPressed)
            //     zx.PokeByteNoContend(23617, 0);

            switch (keyEvent.KeyCode) {
                case Keys.A:
                zx.keyBuffer[(int)keyCode.A] = false;
                break;

                case Keys.B:
                zx.keyBuffer[(int)keyCode.B] = false;
                break;

                case Keys.C:
                zx.keyBuffer[(int)keyCode.C] = false;
                break;

                case Keys.D:
                zx.keyBuffer[(int)keyCode.D] = false;
                break;

                case Keys.E:
                zx.keyBuffer[(int)keyCode.E] = false;
                break;

                case Keys.F:
                zx.keyBuffer[(int)keyCode.F] = false;
                break;

                case Keys.G:
                zx.keyBuffer[(int)keyCode.G] = false;
                break;

                case Keys.H:
                zx.keyBuffer[(int)keyCode.H] = false;
                break;

                case Keys.I:
                zx.keyBuffer[(int)keyCode.I] = false;
                break;

                case Keys.J:
                zx.keyBuffer[(int)keyCode.J] = false;
                break;

                case Keys.K:
                zx.keyBuffer[(int)keyCode.K] = false;
                break;

                case Keys.L:
                zx.keyBuffer[(int)keyCode.L] = false;
                break;

                case Keys.M:
                zx.keyBuffer[(int)keyCode.M] = false;
                break;

                case Keys.N:
                zx.keyBuffer[(int)keyCode.N] = false;
                break;

                case Keys.O:
                zx.keyBuffer[(int)keyCode.O] = false;
                break;

                case Keys.P:
                zx.keyBuffer[(int)keyCode.P] = false;
                break;

                case Keys.Q:
                zx.keyBuffer[(int)keyCode.Q] = false;
                break;

                case Keys.R:
                zx.keyBuffer[(int)keyCode.R] = false;
                break;

                case Keys.S:
                zx.keyBuffer[(int)keyCode.S] = false;
                break;

                case Keys.T:
                zx.keyBuffer[(int)keyCode.T] = false;
                break;

                case Keys.U:
                zx.keyBuffer[(int)keyCode.U] = false;
                break;

                case Keys.V:
                zx.keyBuffer[(int)keyCode.V] = false;
                break;

                case Keys.W:
                zx.keyBuffer[(int)keyCode.W] = false;
                break;

                case Keys.X:
                zx.keyBuffer[(int)keyCode.X] = false;
                break;

                case Keys.Y:
                zx.keyBuffer[(int)keyCode.Y] = false;
                break;

                case Keys.Z:
                zx.keyBuffer[(int)keyCode.Z] = false;
                break;

                case Keys.D0:
                zx.keyBuffer[(int)keyCode._0] = false;
                break;

                case Keys.D1:
                zx.keyBuffer[(int)keyCode._1] = false;
                break;

                case Keys.D2:
                zx.keyBuffer[(int)keyCode._2] = false;
                break;

                case Keys.D3:
                zx.keyBuffer[(int)keyCode._3] = false;
                break;

                case Keys.D4:
                zx.keyBuffer[(int)keyCode._4] = false;
                break;

                case Keys.D5:
                zx.keyBuffer[(int)keyCode._5] = false;
                break;

                case Keys.D6:
                zx.keyBuffer[(int)keyCode._6] = false;
                break;

                case Keys.D7:
                zx.keyBuffer[(int)keyCode._7] = false;
                break;

                case Keys.D8:
                zx.keyBuffer[(int)keyCode._8] = false;
                break;

                case Keys.D9:
                zx.keyBuffer[(int)keyCode._9] = false;
                break;

                case Keys.Enter:
                zx.keyBuffer[(int)keyCode.ENTER] = false;
                break;

                case Keys.Space:
                zx.keyBuffer[(int)keyCode.SPACE] = false;
                break;

                //case Keys.ControlKey:
                //    zx.keyBuffer[(int)keyCode.CTRL] = false;
                //    break;

                case Keys.ShiftKey:
                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy(255, false);
                break;

                case Keys.Up:

                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.UP, false);

                zx.keyBuffer[(int)keyCode.UP] = false;
                break;

                case Keys.Left:

                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.LEFT, false);

                zx.keyBuffer[(int)keyCode.LEFT] = false;
                break;

                case Keys.Right:

                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.RIGHT, false);

                zx.keyBuffer[(int)keyCode.RIGHT] = false;
                break;

                case Keys.Down:

                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy((int)keyCode.DOWN, false);

                zx.keyBuffer[(int)keyCode.DOWN] = false;
                break;

                case Keys.Back:
                zx.keyBuffer[(int)keyCode.BACK] = false;
                break;

                case Keys.Tab:
                zx.keyBuffer[(int)keyCode.TAB] = false;

                if (config.inputDeviceOptions.EnableKey2Joy)
                    HandleKey2Joy(255, false);
                break;

                case Keys.CapsLock:
                zx.keyBuffer[(int)keyCode.CAPS] = true;
                capsLockOn = !capsLockOn;
                break;

                case Keys.OemPeriod:
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode.M] = false;
                zx.keyBuffer[(int)keyCode.T] = false;
                break;

                case Keys.Oemcomma:
                zx.keyBuffer[(int)keyCode.R] = false;
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode.N] = false;
                break;

                case Keys.OemQuotes:
                zx.keyBuffer[(int)keyCode.P] = false;
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode._7] = false;
                break;

                case Keys.OemSemicolon:
                zx.keyBuffer[(int)keyCode.Z] = false;
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode.O] = false;
                break;

                case Keys.OemBackslash:
                zx.keyBuffer[(int)keyCode.C] = false;
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode.V] = false;
                break;

                case Keys.Oem4: //brace open
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.PokeByteNoContend(23617, 0);
                zx.keyBuffer[(int)keyCode.F] = false;
                zx.keyBuffer[(int)keyCode.Y] = false;
                break;

                case Keys.Oem6: //brace close
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.PokeByteNoContend(23617, 0);
                zx.keyBuffer[(int)keyCode.U] = false;
                zx.keyBuffer[(int)keyCode.G] = false;
                break;

                case Keys.OemMinus:
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode._0] = false;
                zx.keyBuffer[(int)keyCode.J] = false;
                break;

                case Keys.Oemplus:
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.keyBuffer[(int)keyCode.K] = false;
                zx.keyBuffer[(int)keyCode.L] = false;
                break;

                case Keys.OemPipe:
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.PokeByteNoContend(23617, 0);
                zx.keyBuffer[(int)keyCode.S] = false;
                zx.keyBuffer[(int)keyCode.D] = false;
                break;

                case Keys.Oemtilde:
                zx.keyBuffer[(int)keyCode.CTRL] = false;
                zx.PokeByteNoContend(23617, 0);
                zx.keyBuffer[(int)keyCode.A] = false;
                break;

                case Keys.F12:
                zx.keyBuffer[(int)keyCode.F12] = false;
                break;

                case Keys.ControlKey:
                // if (shiftIsPressed)
                {
                        zx.keyBuffer[(int)keyCode.CTRL] = false;
                        //  zx.keyBuffer[(int)keyCode.SHIFT] = false;
                    }
                    break;

                default:
                if (keyEvent.KeyValue == 191) //frontslash
                {
                    zx.keyBuffer[(int)keyCode.CTRL] = false;
                    zx.keyBuffer[(int)keyCode.C] = false;
                    zx.keyBuffer[(int)keyCode.V] = false;
                }
                break;
            }
            shiftIsPressed = (Native.GetAsyncKeyState(VK_LSHIFT) & 0x8000) != 0;// (keyEvent.KeyCode & Keys.Shift) != 0;
            ctrlIsPressed = (((Native.GetAsyncKeyState(VK_RSHIFT) & 0x8000) != 0) || ((keyEvent.Modifiers & Keys.Control) != 0));
            altIsPressed = (keyEvent.Modifiers & Keys.Alt) != 0;
            zx.keyBuffer[(int)keyCode.SHIFT] = shiftIsPressed;
            // zx.keyBuffer[(int)keyCode.CTRL] = ctrlIsPressed;

            zx.keyBuffer[(int)keyCode.ALT] = altIsPressed;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
		{
            //base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
		{
            //dxWindow.Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
		{
            //Show the confirmation box only if it's not an invalid ROM exit event and not fullscreen
            if (config.emulationOptions.ConfirmOnExit && romLoaded && !config.renderOptions.FullScreenMode)
			{
				if (System.Windows.Forms.MessageBox.Show("Are you sure you want to exit?",
						   "Confirm Exit", System.Windows.Forms.MessageBoxButtons.YesNo,
						   System.Windows.Forms.MessageBoxIcon.Question) == DialogResult.No)
				{
					e.Cancel = true;
				}
            }
            base.OnFormClosing(e);
        }

        protected override void OnClosed(EventArgs e)
		{
            logger.Log("Shutting down...", true);
            if (traceFile != null && traceFile.BaseStream != null) {
                traceFile.Flush();
                traceFile.Close();
            }

            for (int f = 0; f < 4; f++)
                if (diskArchivePath[f] != null) {
                    zx.DiskEject((byte)f);
                    File.Delete(diskArchivePath[f]);
                }

            if ((config != null))
			{
                if (inputSystem.Joystick_1_Index >= 0 && inputSystem.Joystick_1.isInitialized)
				{
                    config.inputDeviceOptions.Joystick1Name = inputSystem.Joystick_1.name;
                    config.inputDeviceOptions.Joystick1ToEmulate = inputSystem.Joystick_1_MapIndex;
                }
                else {
                    config.inputDeviceOptions.Joystick1Name = "";
                    config.inputDeviceOptions.Joystick1ToEmulate = 0;
                }

                if (inputSystem.Joystick_2_Index >= 0 && inputSystem.Joystick_2.isInitialized) {
                    config.inputDeviceOptions.Joystick2Name = inputSystem.Joystick_2.name;
                    config.inputDeviceOptions.Joystick2ToEmulate = inputSystem.Joystick_2_MapIndex;
                }
                else {
                    config.inputDeviceOptions.Joystick2Name = "";
                    config.inputDeviceOptions.Joystick2ToEmulate = 0;
                }

                config.Save(Application.LocalUserAppDataPath);
            }

            inputSystem.ReleaseResources();

            if (dxWindow != null)
                dxWindow.Shutdown();

            if (zx != null)
                zx.Shutdown();

            //Clean up any temporary files
            var dir = new DirectoryInfo(Application.LocalUserAppDataPath);
            foreach (var file in dir.EnumerateFiles("temp*")) {
                file.Delete();
            }
            base.OnClosed(e);
        }

        private void InitializeSpeccyModel(MachineModel model)
		{
			string srcmethod = $"{nameof(ZXForm)}:{nameof(InitializeLifetimeService)}";
			logger.Log($"{srcmethod} - Switching machine model");
			switch (config.emulationOptions.CurrentModel)
			{
                case MachineModel._48k:
					config.emulationOptions.CurrentModel = MachineModel._48k;
					try
					{
						zx = new zx_48k(logger, this.Handle, config.emulationOptions.LateTimings);
						zx.EnableAY(true); //ZXIT
						zx.EnableULAZX(true);
						zx.EnableLOKEYFPU(true);
						zx.EnableLOKEYIO(true);
						romLoaded = LoadROM(config.romOptions.Current48kROM);
						machineLabel.Text = "Spectrum 48K";
						zxSpectrum48kToolStripMenuItem.Checked = true;
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Exception {ex.ToString()}");
						Application.Exit();
					}
				break;
			}
            zx.FrameEndEvent += OnSpeccyFrameEnd;
        }

        private void Form1_Load(object sender, System.EventArgs e)
		{

            logger.Log("Starting up...");
            this.BringToFront();

			config.Default();

            romLoaded = false;
            recentFolder = config.pathOptions.Programs;

            logger.Log("Powering on the speccy...");

            switch (config.emulationOptions.CurrentModelName)
			{
                case ZX_SPECTRUM_48K:
					config.emulationOptions.CurrentModel = MachineModel._48k;
					config.emulationOptions.CPUMultiplier = LokeyGlobals.lokey_cpu_multiplier;
					break;
            }

            InitializeSpeccyModel(config.emulationOptions.CurrentModel);

            logger.Log("Initializing tape deck...");
            if (!romLoaded)
			{
                this.Close();
                return;
            }

            zx.SetSoundVolume(config.audioOptions.Volume / 100.0f);
            zx.SetEmulationSpeed(config.emulationOptions.EmulationSpeed);
            zx.SetStereoSound(config.audioOptions.StereoSoundMode);

            try
			{
                logger.Log("Initializing direct X renderer...");
				dxWindow = new GDIRenderer(this, panel1.Width, panel1.Height);
			}
			catch (System.TypeInitializationException dxex)
			{
                MessageBox.Show(dxex.InnerException.Message, "Wrong DirectX version.", MessageBoxButtons.OK);
                return;
            }

            if (config.renderOptions.UseDirectX)
                directXToolStripMenuItem_Click(this, null);
            else
                gDIToolStripMenuItem_Click(this, null);

            switch (config.emulationOptions.EmulationSpeed)
			{
                case 1:
                emulationSpeed1_Click(this, null);
                break;
                case 2:
                emulationSpeed2_Click(this, null);
                break;
                case 4:
                emulationSpeed4_Click(this, null);
                break;
                case 8:
                emulationSpeed8_Click(this, null);
                break;
                case 10:
                emulationSpeed10_Click(this, null);
                break;
            }
            
            switch (config.emulationOptions.CPUMultiplier)
			{
                case 1:
                cpuSpeed1_Click(this, null);
                break;
                case 1.25:
                cpuSpeed2_Click(this, null);
                break;
                case 1.5:
                cpuSpeed3_Click(this, null);
                break;
                case 8:
                cpuSpeed4_Click(this, null);
                break;
                case 14:
                cpuSpeed5_Click(this, null);
                break;
            }
            dxWindow.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.Controls.Add(dxWindow);
            panel1.Enabled = false;
            panel1.Hide();
            panel1.SendToBack();
            dxWindow.BringToFront();
            dxWindow.Focus();

            logger.Log("Initializing window...");

            if (config.renderOptions.WindowSize < 0)
			{
                config.renderOptions.WindowSize = 0;
            }
            AdjustWindowSize();

            inputSystem.Init(this);
            inputSystem.SetupJoysticks();
            inputSystem.SetMouseSensitivity(config.inputDeviceOptions.MouseSensitivity);
            
            zx.Start();
		}

		private void Form_MouseDown(object sender, MouseEventArgs e) {
            mouseOrigin.X = e.X;
            mouseOrigin.Y = e.Y;
        }

        private void Form_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.Top += e.Y - mouseOrigin.Y;
                this.Left += e.X - mouseOrigin.X;
            }
        }

        //Load file
        private void fileButton_Click(object sender, EventArgs e) {
            openFileMenuItem1_Click(sender, e);
        }

        private void directXToolStripMenuItem_Click(object sender, EventArgs e)
		{
			dxWindow.Focus();
		}

        private void gDIToolStripMenuItem_Click(object sender, EventArgs e)
		{
            dxWindow.Focus();
            config.renderOptions.UseDirectX = false;
        }

        private void kToolStripMenuItem_Click(object sender, EventArgs e) {
            zx.Reset(false);
            dxWindow.Focus();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e) {
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e) {
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e) {
        }

        //100% window size
        private void size100ToolStripMenuItem_Click(object sender, EventArgs e)
		{
            //if (config.renderOptions.FullScreenMode)
            //    GoFullscreen(false);

            config.renderOptions.WindowSize = 0;
            AdjustWindowSize();
        }

        protected override bool ProcessDialogKey(Keys keyData) {
            return false;
        }

        protected override bool ProcessCmdKey(ref
              System.Windows.Forms.Message m,
              System.Windows.Forms.Keys k) {
            // detect the pushing (Msg) of Enter Key (k)

            // then process the signal as usual
            return base.ProcessCmdKey(ref m, k);
        }

        //Monitor
        private void monitorButton_Click(object sender, EventArgs e) {
            ShouldExitFullscreen();

            if (debugger == null || debugger.IsDisposed) {
                debugger = new Monitor(this);
                debugger.SetState(Monitor.MonitorState.PAUSE);
                debugger.Show();
            }

            if (!debugger.Visible) {
                debugger.ReSyncWithZX();
                debugger.SetState(Monitor.MonitorState.PAUSE);
                debugger.Show();
            }
            debugger.BringToFront();
        }

        //48k select
        public void zx48ktoolStripMenuItem1_Click(object sender, EventArgs e)
		{
            ChangeSpectrumModel(MachineModel._48k);
            config.emulationOptions.CurrentModelName = ZX_SPECTRUM_48K;
            zxSpectrum48kToolStripMenuItem.Checked = true;
        }

        private void ChangeSpectrumModel(MachineModel _model)
		{
			if (softResetOnly)
			{
				return;
			}
            zx.Pause();
            SetEmulationState(EMULATOR_STATE.IDLE);
            dxWindow.Suspend();
            softResetOnly = false;
            if (debugger != null) {
                debugger.DeRegisterAllEvents();
                debugger.DeSyncWithZX();
            }

            zx.Shutdown();
            zx = null;

            System.GC.Collect();

            config.emulationOptions.CurrentModel = _model;
            Directory.SetCurrentDirectory(Application.StartupPath);

            InitializeSpeccyModel(config.emulationOptions.CurrentModel);

            if (!romLoaded) {
                this.Close();
                return;
            }

            zx.SetSoundVolume(config.audioOptions.Volume / 100.0f);
            zx.SetEmulationSpeed(config.emulationOptions.EmulationSpeed);
            zx.SetCPUSpeed(config.emulationOptions.CPUMultiplier);
            zx.SetStereoSound(config.audioOptions.StereoSoundMode);

            inputSystem.EnableJoystick();

            zx.MuteSound(config.audioOptions.Mute);
            if (!config.audioOptions.Mute)
                soundStatusLabel.Image = Properties.Resources.sound_high;
            else
                soundStatusLabel.Image = Properties.Resources.sound_mute;

            if (debugger != null) {
                debugger.ReRegisterAllEvents();
                debugger.ReSyncWithZX();
            }

            dxWindow.Resume();
            dxWindow.Focus();
            zx.Resume();
        }

        //options button
        private void optionsButton_Click(object sender, EventArgs e) {
            if ((optionWindow == null) || (optionWindow.IsDisposed))
                optionWindow = new Options(this);
            bool oldPause = pauseEmulation;
            pauseEmulation = true;
            zx.Pause();
            PreOptionsWindowShow();
            if (optionWindow.ShowDialog(this) == DialogResult.OK) {
                PostOptionsWindowShow();
            }
            pauseEmulation = oldPause;
            zx.Resume();
            dxWindow.Focus();
        }

        private void PreOptionsWindowShow()
		{
            optionWindow.RomToUse48k = config.romOptions.Current48kROM;

            optionWindow.SpectrumModel = GetSpectrumModelIndex(config.emulationOptions.CurrentModelName);
            optionWindow.UseIssue2Keyboard = config.emulationOptions.UseIssue2Keyboard;

            optionWindow.FileAssociateSNA = config.fileAssociationOptions.AccociateSNAFiles;
            optionWindow.FileAssociateZ80 = config.fileAssociationOptions.AccociateZ80Files;

            optionWindow.SpeakerSetup = config.audioOptions.StereoSoundMode;

            optionWindow.KempstonUsesPort1F = config.inputDeviceOptions.KempstonUsesPort1F;
            optionWindow.EnableKey2Joy = config.inputDeviceOptions.EnableKey2Joy;
            optionWindow.Key2JoyStickType = config.inputDeviceOptions.Key2JoystickType - 1;
            optionWindow.EnableKempstonMouse = config.inputDeviceOptions.EnableKempstonMouse;
            optionWindow.MouseSensitivity = config.inputDeviceOptions.MouseSensitivity;

            optionWindow.UseDirectX = config.renderOptions.UseDirectX;
            optionWindow.InterlacedMode = config.renderOptions.Scanlines;
            //optionWindow.PixelSmoothing = dxWindow.PixelSmoothing; ZXIT
            optionWindow.EnableVSync = config.renderOptions.Vsync;
            optionWindow.MaintainAspectRatioInFullScreen = config.renderOptions.MaintainAspectRatioInFullScreen;

            switch (config.renderOptions.Palette)
			{
                case "Grayscale":
                optionWindow.Palette = 1;
                break;

                case "ULA Plus":
                optionWindow.Palette = 2;
                break;

                default:
                optionWindow.Palette = 0;
                break;
            }

            optionWindow.borderSize = config.renderOptions.BorderSize / 24;
            optionWindow.UseLateTimings = config.emulationOptions.LateTimings;
            optionWindow.PauseOnFocusChange = config.emulationOptions.PauseOnFocusLost;
            optionWindow.ConfirmOnExit = config.emulationOptions.ConfirmOnExit;

			//48k snapshot sometimes enable AY sound, so we retain the state from the current running instance
			optionWindow.EnableAYFor48K = true; //ZXIT //zx.HasAYSound; 

            optionWindow.Joystick1Choice = inputSystem.Joystick_1_Index + 1;
            optionWindow.Joystick2Choice = inputSystem.Joystick_2_Index + 1;
            optionWindow.HighCompatibilityMode = config.emulationOptions.Use128keForSnapshots;
            optionWindow.RomPath = config.pathOptions.Roms;
            optionWindow.GamePath = config.pathOptions.Programs;
            optionWindow.Joystick1EmulationChoice = inputSystem.Joystick_1_MapIndex;
            optionWindow.Joystick2EmulationChoice = inputSystem.Joystick_2_MapIndex;
            //optionWindow.ShowOnScreenLEDS = config.ShowOnscreenIndicators;
            optionWindow.RestoreLastState = config.emulationOptions.RestorePreviousSessionOnStart;

			if (config.renderOptions.FullScreenMode)
			{
				optionWindow.windowSize = 0;
			}
			else
			{
				optionWindow.windowSize = config.renderOptions.WindowSize / 50 + 1;
			}
        }

        private void PostOptionsWindowShow() {
            config.emulationOptions.UseIssue2Keyboard = optionWindow.UseIssue2Keyboard;
            config.emulationOptions.LateTimings = (optionWindow.UseLateTimings);// == true ? 1 : 0);
            config.emulationOptions.Use128keForSnapshots = optionWindow.HighCompatibilityMode;
            config.emulationOptions.RestorePreviousSessionOnStart = optionWindow.RestoreLastState;

            config.renderOptions.MaintainAspectRatioInFullScreen = optionWindow.MaintainAspectRatioInFullScreen;
            config.renderOptions.UseDirectX = optionWindow.UseDirectX;
            config.renderOptions.Scanlines = optionWindow.InterlacedMode;

            config.pathOptions.Roms = optionWindow.RomPath;
            config.pathOptions.Programs = optionWindow.GamePath;

			config.audioOptions.EnableAYFor48K = true; //ZXIT
            config.audioOptions.StereoSoundMode = optionWindow.SpeakerSetup;

            config.inputDeviceOptions.EnableKempstonMouse = optionWindow.EnableKempstonMouse;
            config.inputDeviceOptions.MouseSensitivity = optionWindow.MouseSensitivity;         
            config.inputDeviceOptions.KempstonUsesPort1F = optionWindow.KempstonUsesPort1F;
            config.inputDeviceOptions.EnableKey2Joy = optionWindow.EnableKey2Joy;
            config.inputDeviceOptions.Key2JoystickType = optionWindow.Key2JoyStickType + 1;

            if (config.renderOptions.Vsync != optionWindow.EnableVSync)
			{
                config.renderOptions.Vsync = optionWindow.EnableVSync;
            }

            //Remove any previous session info if user doesn't want restore function
            if (!config.emulationOptions.RestorePreviousSessionOnStart) {
                if (File.Exists(ZeroSessionSnapshotName))
                    File.Delete(ZeroSessionSnapshotName);
            }

            if (config.renderOptions.UseDirectX)
                directXToolStripMenuItem_Click(this, null);
            else
                gDIToolStripMenuItem_Click(this, null);

            if ((optionWindow.SpectrumModel != GetSpectrumModelIndex(config.emulationOptions.CurrentModelName))
                 || config.romOptions.Current48kROM != optionWindow.RomToUse48k )
			{
                config.romOptions.Current48kROM = optionWindow.RomToUse48k;

                switch (optionWindow.SpectrumModel)
				{
                    case 0:
                    zx48ktoolStripMenuItem1_Click(this, null);
                    break;
                }
            }
            zx.Issue2Keyboard = config.emulationOptions.UseIssue2Keyboard;
            zx.LateTiming = (config.emulationOptions.LateTimings ? 1 : 0);
            config.emulationOptions.ConfirmOnExit = optionWindow.ConfirmOnExit;
            config.emulationOptions.PauseOnFocusLost = optionWindow.PauseOnFocusChange;
			zx.EnableAY(true);//ZXIT
            zx.SetStereoSound(config.audioOptions.StereoSoundMode); //Also sets ACB/ABC config internally

            inputSystem.Joystick_1_Index = optionWindow.Joystick1Choice - 1;
            inputSystem.Joystick_2_Index = optionWindow.Joystick2Choice - 1;
            inputSystem.Joystick_1_MapIndex = optionWindow.Joystick1EmulationChoice;
            inputSystem.Joystick_2_MapIndex = optionWindow.Joystick2EmulationChoice;
            inputSystem.SetupJoysticks();
            inputSystem.SetMouseSensitivity(config.inputDeviceOptions.MouseSensitivity);

            CheckFileAssociations();
            optionWindow.Dispose();

            bool requiresResizeWindow = false;

            //Are we going windowed from fullscreen?
            if (optionWindow.windowSize != 0 && config.renderOptions.FullScreenMode)
			{
                config.renderOptions.WindowSize = (optionWindow.windowSize - 1) * 50;
                GoFullscreen(config.renderOptions.FullScreenMode);
            }
            else if (optionWindow.windowSize == 0 && !config.renderOptions.FullScreenMode) //or the other way
            {
                //GoFullscreen(config.renderOptions.FullScreenMode);
            }
            else if (optionWindow.windowSize > 0 && (config.renderOptions.WindowSize != (optionWindow.windowSize - 1) * 50)) //or diff window size from previous
            {
                config.renderOptions.WindowSize = (optionWindow.windowSize - 1) * 50;
                requiresResizeWindow = true;
            }

            //Change in border size?
            if (config.renderOptions.BorderSize != (optionWindow.borderSize * 24)) {
                config.renderOptions.BorderSize = optionWindow.borderSize * 24;
                requiresResizeWindow = true;
            }

            if (requiresResizeWindow)
                AdjustWindowSize();

            //dxWindow.PixelSmoothing = config.renderOptions.PixelSmoothing; //ZXIT
        }

        //Need this to command the windows explorer shell to refresh icon cache
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        public void CheckFileAssociations() {
            string param = "";
            string assoc = "";
            bool iconsChanged = false;

            if (optionWindow.FileAssociateDSK != config.fileAssociationOptions.AccociateDSKFiles) {
                assoc = (optionWindow.FileAssociateDSK ? " 1.dsk" : " 0.dsk");
                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateTRD != config.fileAssociationOptions.AccociateTRDFiles) {
                assoc = (optionWindow.FileAssociateTRD ? " 1.trd" : " 0.trd");
                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateSCL != config.fileAssociationOptions.AccociateSCLFiles) {
                assoc = (optionWindow.FileAssociateSCL ? " 1.scl" : " 0.scl");
                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociatePZX != config.fileAssociationOptions.AccociatePZXFiles) {
                assoc = (optionWindow.FileAssociatePZX ? " 1.pzx" : " 0.pzx");

                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateTZX != config.fileAssociationOptions.AccociateTZXFiles) {
                assoc = (optionWindow.FileAssociateTZX ? " 1.tzx" : " 0.tzx");

                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateTAP != config.fileAssociationOptions.AccociateTAPFiles) {
                assoc = (optionWindow.FileAssociateTAP ? " 1.tap" : " 0.tap");

                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateSNA != config.fileAssociationOptions.AccociateSNAFiles) {
                assoc = (optionWindow.FileAssociateSNA ? " 1.sna" : " 0.sna");

                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateSZX != config.fileAssociationOptions.AccociateSZXFiles) {
                assoc = (optionWindow.FileAssociateSZX ? " 1.szx" : " 0.szx");

                iconsChanged = true;
                param += assoc;
            }

            if (optionWindow.FileAssociateZ80 != config.fileAssociationOptions.AccociateZ80Files) {
                assoc = (optionWindow.FileAssociateZ80 ? " 1.z80" : " 0.z80");
                iconsChanged = true;
                param += assoc;
            }

            int exitCode = -1;
            //Force icon refresh in windows explorer shell
            if (iconsChanged) {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Application.StartupPath;
                startInfo.FileName = "ZeroFileAssociater";
                startInfo.Arguments = param;
                startInfo.Verb = "runas";
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; //run silent, run deep...
                try {
                    System.Diagnostics.Process p = System.Diagnostics.Process.Start(startInfo);
                    if (p != null)
                        p.WaitForExit();
                    exitCode = p.ExitCode;
                }
                catch (Exception e) {
                    MessageBox.Show(e.Message, "ZeroFileAssociater launch failed!", MessageBoxButtons.OK);
                    dxWindow.Focus();
                    return;
                }
            }

            //file associations updated successfully!
            if (exitCode == 0) {
                config.fileAssociationOptions.AccociatePZXFiles = optionWindow.FileAssociatePZX;
                config.fileAssociationOptions.AccociateTZXFiles = optionWindow.FileAssociateTZX;
                config.fileAssociationOptions.AccociateTAPFiles = optionWindow.FileAssociateTAP;
                config.fileAssociationOptions.AccociateSNAFiles = optionWindow.FileAssociateSNA;
                config.fileAssociationOptions.AccociateSZXFiles = optionWindow.FileAssociateSZX;
                config.fileAssociationOptions.AccociateZ80Files = optionWindow.FileAssociateZ80;
                config.fileAssociationOptions.AccociateDSKFiles = optionWindow.FileAssociateDSK;
                config.fileAssociationOptions.AccociateTRDFiles = optionWindow.FileAssociateTRD;
                config.fileAssociationOptions.AccociateSCLFiles = optionWindow.FileAssociateSCL;

                SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                MessageBox.Show("File associations updated successfully!", "File associations", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            dxWindow.Focus();
        }

        private void aboutButton_Click(object sender, EventArgs e) {
            if ((aboutWindow == null) || (aboutWindow.IsDisposed))
                aboutWindow = new AboutBox1(this);
            aboutWindow.ShowDialog(this);
            dxWindow.Focus();
            aboutWindow.Dispose();
        }

        private void label1_Click(object sender, EventArgs e) {
            dxWindow.Focus();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
		{
            zx.Reset(false);
            dxWindow.Focus();
        }

        private void renderingToolStripMenuItem_Paint(object sender, PaintEventArgs e) {
        }

        //power off
        private void powerButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void hardResetToolStripMenuItem1_Click(object sender, EventArgs e)
		{
            SetEmulationState(EMULATOR_STATE.IDLE);
            dxWindow.Suspend();
            if (debugger != null)
			{
                debugger.DeRegisterAllEvents();
                debugger.DeSyncWithZX();
            }

            zx.Reset(true);

            if (debugger != null) {
                debugger.ReRegisterAllEvents();
                debugger.ReSyncWithZX();
            }

            dxWindow.Resume();
            dxWindow.Focus();
        }

        public void ShouldExitFullscreen()
		{
			//if (dxWindow.EnableDirectX && config.renderOptions.FullScreenMode)
			//{
			//	GoFullscreen(false);
			//} //ZXIT
        }

        private void GoFullscreen(bool full)
		{
            config.renderOptions.FullScreenMode = false;
			return;

            if(full)
			{
                statusStrip1.Visible = false;
                toolStripMenuItem5.Enabled = false;
                toolStripMenuItem1.Enabled = false;
                LastMouseMove = DateTime.Now;
                this.SuspendLayout();
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                oldWindowPosition = this.Location;
                // dxWindow.EnableFullScreen = true; //ZXIT
                oldWindowSize = config.renderOptions.WindowSize;
                config.renderOptions.WindowSize = 0;

    //            if (dxWindow.EnableDirectX)
				//{
    //                menuStrip1.Visible = false;
    //                dxWindow.InitDirectX(Screen.FromControl(this).Bounds.Width, Screen.FromControl(this).Bounds.Height, false);
    //            }
    //            else
				//{
    //                this.Location = new Point(0, 0);
    //                this.WindowState = FormWindowState.Maximized;
    //                dxWindow.Location = new Point(0, 0);
    //                dxWindow.SetSize(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
    //            } //ZXIT
				this.Location = new Point(0, 0);
				this.WindowState = FormWindowState.Maximized;
				dxWindow.Location = new Point(0, 0);
				dxWindow.SetSize(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

				if (!commandLineLaunch) {
                    Point cursorPos = Cursor.Position;
                    cursorPos.Y = this.PointToScreen(dxWindow.Location).Y;
                    Cursor.Position = cursorPos;
                }
                else {
                    Cursor.Hide();
                    CursorIsHidden = true;
                    Cursor.Position = new Point(0, Screen.PrimaryScreen.Bounds.Height);
                    mouseOldPos.X = Cursor.Position.X;
                    mouseOldPos.Y = Cursor.Position.Y;
                }
                this.ResumeLayout();
                dxWindow.Focus();
            }
            else
			{
                menuStrip1.Visible = true;
                statusStrip1.Visible = true;
                toolStripMenuItem5.Enabled = true;
                toolStripMenuItem1.Enabled = true;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                this.WindowState = FormWindowState.Normal;

                //dxWindow.EnableFullScreen = false; //ZXIT
                this.Location = oldWindowPosition;
                config.renderOptions.WindowSize = oldWindowSize;
                AdjustWindowSize();

                if (CursorIsHidden) {
                    Cursor.Show();
                    CursorIsHidden = false;
                }
            }
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
            //GoFullscreen(!config.renderOptions.FullScreenMode);
        }

        private void PauseEmulation(bool val)
		{
            pauseEmulation = val;
            //dxWindow.EmulationIsPaused = val; //ZXIT
            dxWindow.Invalidate();
			toolStripMenuItem6.Checked = val;
            SetEmulationState((pauseEmulation ? EMULATOR_STATE.PAUSED : prevState));
        }


        private void pauseEmulationESCToolStripMenuItem_Click(object sender, EventArgs e) {
            PauseEmulation(!pauseEmulation);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e) {
            powerButton_Click(this, null);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e) {
            mouseMoveDiff.X = e.Location.X - mouseOldPos.X;
            mouseMoveDiff.Y = e.Location.Y - mouseOldPos.Y;

            LastMouseMove = DateTime.Now;
   //         if (config.renderOptions.FullScreenMode)
			//{
   //             if ((Math.Abs(mouseMoveDiff.X)) > 5 || (Math.Abs(mouseMoveDiff.Y) > 5)) {
   //                 if (CursorIsHidden) {
   //                     Cursor.Show();
   //                     CursorIsHidden = false;
   //                 }
   //             }
   //         }
            mouseOldPos = e.Location;
        }

        private void libraryButton_Click(object sender, EventArgs e) {
        }

        private void Form1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                LoadZXFile(files[0]);
            }
        }

        private void UseSNA(SNA_SNAPSHOT sna)
		{
            if (sna == null)
                return;

            if (sna is SNA_48K)
			{
                zx48ktoolStripMenuItem1_Click(this, null);
            }
            zx.UseSNA(sna);
        }
        public void LoadSNA(ref byte[] buffer) {
            SNA_SNAPSHOT sna = SNAFile.LoadSNA(ref buffer);
            UseSNA(sna);
        }

        public void LoadSNA(Stream fs, int stream_length) {
            SNA_SNAPSHOT sna = SNAFile.LoadSNA(fs);
            UseSNA(sna);
        }

        public void LoadSNA(string filename)
		{
            SNA_SNAPSHOT sna = SNAFile.LoadSNA(filename);
            UseSNA(sna);
        }

        private void UseZ80(Z80_SNAPSHOT z80)
		{
            if (z80 == null)
                return;

            if (z80.TYPE == 0)
			{
                zx48ktoolStripMenuItem1_Click(this, null);
            }

            zx.UseZ80(z80);
            z80 = null;
        }

        public void LoadZ80(ref byte[] buffer)
		{
            Z80_SNAPSHOT z80 = Z80File.LoadZ80(ref buffer);
            UseZ80(z80);
        }

        public void LoadZ80(Stream fs)
		{
            Z80_SNAPSHOT z80 = Z80File.LoadZ80(fs);
            UseZ80(z80);
        }

        public void LoadZ80(string filename)
		{
            Z80_SNAPSHOT z80 = Z80File.LoadZ80(filename);
            UseZ80(z80);
        }

        public void LoadZXFile(string filename) {
            if (!System.IO.File.Exists(filename))
			{
                MessageBox.Show("Unable to open file: " + filename, "File error", MessageBoxButtons.OK);
                return;
            }
            String ext = System.IO.Path.GetExtension(filename).ToLower();

			if (ext == ".scr")
			{
				using (FileStream fs = new FileStream(filename, FileMode.Open))
				{
					using (BinaryReader br = new BinaryReader(fs))
					{
						byte[] buffer = new byte[6912];
						int bytesRead = br.Read(buffer, 0, 6912);

						if (fs.Length > 6912 || bytesRead == 0)
						{
							MessageBox.Show("This file seems to have an unsupported screen format.", "File error", MessageBoxButtons.OK);
						}
						else
						{
							for (int f = 0; f < 6912; f++)
							{
								zx.PokeByteNoContend(16384 + f, buffer[f]);
							}
						}
					}
				}
			}
			else if (ext == ".sna")
			{
				LoadSNA(filename);
			}
			else if (ext == ".z80")
			{
				LoadZ80(filename);
			}
			else
			{
				MessageBox.Show("Sorry, but Zero doesn't recognise this file format.","Unsupported Format", MessageBoxButtons.OK);
			}   
        }

        private void loadBinaryMenuItem1_Click(object sender, EventArgs e) {
            loadBinaryDialog = new LoadBinary(this, true);
            loadBinaryDialog.Show();
        }

        private void saveBinaryMenuItem5_Click(object sender, EventArgs e) {
            loadBinaryDialog = new LoadBinary(this, false);
            loadBinaryDialog.Show();
        }

        public void saveSnapshotMenuItem_Click(object sender, EventArgs e) {
            ShouldExitFullscreen();

            saveFileDialog1.Title = "Save Snapshot";
            saveFileDialog1.FileName = "";
            saveFileDialog1.Filter = "SNA | *.sna";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				zx.SaveSNA(saveFileDialog1.FileName);
				MessageBox.Show("Snapshot saved!", "File saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void openFileMenuItem1_Click(object sender, EventArgs e)
		{
            ShouldExitFullscreen();

            zx.Pause();
            dxWindow.Suspend();
            openFileDialog1.InitialDirectory = recentFolder;
            openFileDialog1.Title = "Choose a file";
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "All supported files|*.sna;*.z80;*.scr|Snapshots (*.sna, *.z80)|*.sna;*.z80|Spectrum Screen (*.scr)|*.scr";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
                recentFolder = Path.GetDirectoryName(openFileDialog1.FileName);
                LoadZXFile(openFileDialog1.FileName);
            }
            dxWindow.Resume();
            zx.Resume();
            dxWindow.Focus();
        }

        //The app window, including the emulator window and all the buttons, panels et al
        private void AdjustWindowSize()
		{
            fullScreenToolStripMenuItem.Checked = false;

            int _offsetX = zx.GetOriginOffsetX();
            int _offsetY = zx.GetOriginOffsetY();
            int speccyWidth = zx.GetTotalScreenWidth() - _offsetX;
            int speccyHeight = zx.GetTotalScreenHeight() - _offsetY;
            int dxWindowOffsetX = 10;

            Rectangle screenRectangle = RectangleToScreen(this.ClientRectangle);

            int titleHeight = screenRectangle.Top - this.Top;
            int totalClientWidth = speccyWidth + dxWindowOffsetX + 6;
            int totalClientHeight = speccyHeight + statusStrip1.Height + titleHeight + 8;
            int adjustWidth = (speccyWidth * config.renderOptions.WindowSize / 100);
            int adjustHeight = (speccyHeight * config.renderOptions.WindowSize / 100);

            borderAdjust = config.renderOptions.BorderSize + config.renderOptions.BorderSize * (config.renderOptions.WindowSize / 100);

            this.Size = new Size(totalClientWidth + adjustWidth - (2 * borderAdjust) + _offsetX * 2, totalClientHeight + adjustHeight - (2 * borderAdjust));
            dxWindow.Location = new Point(_offsetX - borderAdjust, _offsetY );//- borderAdjust
			dxWindow.SetSize(zx.GetTotalScreenWidth() + adjustWidth, zx.GetTotalScreenHeight() + adjustHeight);
            dxWindow.SendToBack();

            dxWindow.Focus();
            dxWindow.Invalidate();

			Rectangle workingArea = Screen.GetWorkingArea(this);

            if ((totalClientWidth + (speccyWidth * (config.renderOptions.WindowSize + 50)) / 100 - (2 * borderAdjust) >= workingArea.Width) ||
                ((totalClientHeight + (speccyHeight * (config.renderOptions.WindowSize + 50)) / 100 - (2 * borderAdjust)) >= workingArea.Height)) {
                toolStripMenuItem5.Enabled = false;
            }
            else {
                if (config.renderOptions.WindowSize >= 500)
                    toolStripMenuItem5.Enabled = false;
                else
                    toolStripMenuItem5.Enabled = true;
            }

            if (config.renderOptions.WindowSize == 0)
                toolStripMenuItem1.Enabled = false;
            else
                toolStripMenuItem1.Enabled = true;
        }

        private void toolStripMenuItem5_Click_1(object sender, EventArgs e) {
            if (!toolStripMenuItem5.Enabled)
			{
                MessageBox.Show("Emulator window cannot be resized beyond your monitor's maximum screen dimensions.", "Window Size", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            config.renderOptions.WindowSize += 50; //Increase window size by 50% of normal

            AdjustWindowSize();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{
            if (!toolStripMenuItem1.Enabled)
                return;
            if (config.renderOptions.WindowSize > 0)
                config.renderOptions.WindowSize -= 50;

            AdjustWindowSize();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
		{
            if ((aboutWindow == null) || (aboutWindow.IsDisposed))
                aboutWindow = new AboutBox1(this);
            aboutWindow.ShowDialog(this);
            dxWindow.Focus();
        }

        private void aboutZeroToolStripMenuItem_Click(object sender, EventArgs e)
		{
            if (aboutWindow == null)
                aboutWindow = new AboutBox1(this);
            aboutWindow.ShowDialog(this);
            dxWindow.Focus();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
		{
            ShouldExitFullscreen();

            if (speccyKeyboard == null || speccyKeyboard.IsDisposed)
                speccyKeyboard = new SpectrumKeyboard(this);

            speccyKeyboard.Show();
            speccyKeyboard.BringToFront();
        }

        private void libraryToolStripMenuItem_Click(object sender, EventArgs e) {
            library = new ZLibrary();
            library.Show();
        }


        private void soundStatusLabel_Click(object sender, EventArgs e) {
            zx.MuteSound(config.audioOptions.Mute);
            config.audioOptions.Mute = !config.audioOptions.Mute;
            if (!config.audioOptions.Mute)
                    soundStatusLabel.Image = Properties.Resources.sound_high;
                else
                    soundStatusLabel.Image = Properties.Resources.sound_mute;
        }

        private void Form1_ResizeBegin(object sender, EventArgs e){ isResizing = true; }

        private void Form1_ResizeEnd(object sender, EventArgs e) { isResizing = false; }

        private void statusStrip1_Resize(object sender, EventArgs e) { }

        private void Form1_KeyUp(object sender, KeyEventArgs e) {
        }

        private void UpdateSpeedStatusLabel()
		{
            switch (zx.cpuMultiplier)
			{
                case 1:
                cpuSpeed = "3.5 MHz";
                break;
                case 1.25:
                cpuSpeed = "4.375 MHz";            
                break;
                case 1.5:
                cpuSpeed = "5.25 Mhz";
                break;
                case 2:
                cpuSpeed = "7 MHz";
                break;
                case 3:
                cpuSpeed = "10.5 MHz";
                break;
				case 8:
					cpuSpeed = "28.0 MHz";
					break;
				default:
                cpuSpeed = "28 MHz";
                break;

            }
            speedStatusLabel.Text = (100 * zx.emulationSpeed).ToString() + "% @ " + cpuSpeed;
        }

        //3.5 MHz
        private void cpuSpeed1_Click(object sender, EventArgs e) {
            cpuSpeed1.Checked = true;
            cpuSpeed2.Checked = false;
            cpuSpeed3.Checked = false;
            cpuSpeed4.Checked = false;
            cpuSpeed5.Checked = false;
            config.emulationOptions.CPUMultiplier = 1;
            zx.SetCPUSpeed(1);
            UpdateSpeedStatusLabel();
        }

        //4.375 MHz
        private void cpuSpeed2_Click(object sender, EventArgs e)
		{
            cpuSpeed1.Checked = false;
            cpuSpeed2.Checked = true;
            cpuSpeed3.Checked = false;
            cpuSpeed4.Checked = false;
            cpuSpeed5.Checked = false;
            config.emulationOptions.CPUMultiplier = 1.25;
            zx.SetCPUSpeed(1.25);
            UpdateSpeedStatusLabel();
        }

        //5.25 MHz
        private void cpuSpeed3_Click(object sender, EventArgs e)
		{
            cpuSpeed1.Checked = false;
            cpuSpeed2.Checked = false;
            cpuSpeed3.Checked = true;
            cpuSpeed4.Checked = false;
            cpuSpeed5.Checked = false;
            config.emulationOptions.CPUMultiplier = 1.5;
            zx.SetCPUSpeed(1.5);
            UpdateSpeedStatusLabel();
        }

        //7 MHz
        private void cpuSpeed4_Click(object sender, EventArgs e)
		{
            cpuSpeed1.Checked = false;
            cpuSpeed2.Checked = false;
            cpuSpeed3.Checked = false;
            cpuSpeed4.Checked = true;
            cpuSpeed5.Checked = false;
            config.emulationOptions.CPUMultiplier = 2;
            zx.SetCPUSpeed(2);
            UpdateSpeedStatusLabel();
        }

		//35 Mhz
		private void cpuSpeed5_Click(object sender, EventArgs e)
		{
            cpuSpeed1.Checked = false;
            cpuSpeed2.Checked = false;
            cpuSpeed3.Checked = false;
            cpuSpeed4.Checked = false;
            cpuSpeed5.Checked = true;
            config.emulationOptions.CPUMultiplier = 8;
            zx.SetCPUSpeed(8);
            UpdateSpeedStatusLabel();
        }

        private void emulationSpeed1_Click(object sender, EventArgs e)
		{
            emulationSpeed1.Checked = true;
            emulationSpeed2.Checked = false;
            emulationSpeed4.Checked = false;
            emulationSpeed8.Checked = false;
            emulationSpeed10.Checked = false;
            config.emulationOptions.EmulationSpeed = 1;
            zx.SetEmulationSpeed(1);
            UpdateSpeedStatusLabel();
        }

        private void emulationSpeed2_Click(object sender, EventArgs e) {
            emulationSpeed1.Checked = false;
            emulationSpeed2.Checked = true;
            emulationSpeed4.Checked = false;
            emulationSpeed8.Checked = false;
            emulationSpeed10.Checked = false;
            config.emulationOptions.EmulationSpeed = 2;
            zx.SetEmulationSpeed(2);
            UpdateSpeedStatusLabel();
        }

        private void emulationSpeed4_Click(object sender, EventArgs e) {
            emulationSpeed1.Checked = false;
            emulationSpeed2.Checked = false;
            emulationSpeed4.Checked = true;
            emulationSpeed8.Checked = false;
            emulationSpeed10.Checked = false;
            config.emulationOptions.EmulationSpeed = 4;
            zx.SetEmulationSpeed(4);
            UpdateSpeedStatusLabel();
        }

        private void emulationSpeed8_Click(object sender, EventArgs e) {
            emulationSpeed1.Checked = false;
            emulationSpeed2.Checked = false;
            emulationSpeed4.Checked = false;
            emulationSpeed8.Checked = true;
            emulationSpeed10.Checked = false;
            config.emulationOptions.EmulationSpeed = 8;
            zx.SetEmulationSpeed(8);
            UpdateSpeedStatusLabel();
        }

        private void emulationSpeed10_Click(object sender, EventArgs e) {
            emulationSpeed1.Checked = false;
            emulationSpeed2.Checked = false;
            emulationSpeed4.Checked = false;
            emulationSpeed8.Checked = false;
            emulationSpeed10.Checked = true;
            config.emulationOptions.EmulationSpeed = 10;
            zx.SetEmulationSpeed(10);
            UpdateSpeedStatusLabel();
        }
	}
}