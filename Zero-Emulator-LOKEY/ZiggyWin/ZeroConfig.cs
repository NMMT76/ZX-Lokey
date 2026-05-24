using SpeccyCommon;
using Newtonsoft.Json;
using System.Text;
using LOKEY;

namespace ZeroWin
{
    public class PathSettings
    {
        public string Application { get; set; }
        public string Roms { get; set; }
        public string Programs { get; set; }
        public string Saves { get; set; }
        public string Screenshots { get; set; }
    }

    public class ROMSettings
    {
        public string Current48kROM { get; set; }
    }

    public class RenderSettings
	{
		private bool _fullscreenmode=false;
        public bool FullScreenMode { get { return _fullscreenmode; } set { _fullscreenmode = false; } }
        public bool MaintainAspectRatioInFullScreen { get; set; }
        public bool UseDirectX { get; set; }
        public bool PixelSmoothing { get; set; }
        public bool Scanlines { get; set; }
        public bool Vsync { get; set; }
        public string Palette { get; set; }
        public int BorderSize { get; set; }
        public int WindowSize { get; set; }
    }

    public class AudioSettings
    {
        public int Volume { get; set; }
        public bool Mute { get; set; }
        public bool EnableAYFor48K { get; set; }

        //Speaker setup: 0 = Mono, 1 = ACB, 2 = ABC
        public int StereoSoundMode { get; set; }

    }

    public class EmulationSettings
    {
        public bool Use128keForSnapshots { get; set; }
        public bool UseIssue2Keyboard { get; set; }
        public bool LateTimings { get; set; }
        public bool PauseOnFocusLost { get; set; }
        public bool ConfirmOnExit { get; set; }
        public bool RestorePreviousSessionOnStart { get; set; }
        public double CPUMultiplier { get; set; } //ZXIT
        public int EmulationSpeed { get; set; }
        public string CurrentModelName { get; set; }
        public MachineModel CurrentModel { get; set; }
    }

    public class InputDeviceSettings
    {
        public bool EnableKempstonMouse { get; set; }
        public bool EnableKey2Joy { get; set; }
        public bool KempstonUsesPort1F { get; set; }
        public int MouseSensitivity { get; set; }
        public int Key2JoystickType { get; set; }
        public int Joystick1ToEmulate { get; set; }
        public int Joystick2ToEmulate { get; set; }
        public string Joystick1Name { get; set; }
        public string Joystick2Name { get; set; }
    }

    public class FileAssociationSettings
    {
        public bool AccociateCSWFiles { get; set; }
        public bool AccociatePZXFiles { get; set; }
        public bool AccociateTZXFiles { get; set; }
        public bool AccociateTAPFiles { get; set; }
        public bool AccociateSNAFiles { get; set; }
        public bool AccociateSZXFiles { get; set; }
        public bool AccociateZ80Files { get; set; }
        public bool AccociateDSKFiles { get; set; }
        public bool AccociateTRDFiles { get; set; }
        public bool AccociateSCLFiles { get; set; }
    }

    public class RecentFiles
    {
        public System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();
    }
    public class ZeroConfig
    {
        #region properties
        public PathSettings pathOptions = new PathSettings();
        public ROMSettings romOptions = new ROMSettings();
        public RenderSettings renderOptions = new RenderSettings();
        public AudioSettings audioOptions = new AudioSettings();
        public EmulationSettings emulationOptions = new EmulationSettings();
        public InputDeviceSettings inputDeviceOptions = new InputDeviceSettings();
        public FileAssociationSettings fileAssociationOptions = new FileAssociationSettings();
        public RecentFiles recentFiles = new RecentFiles();
        public void Default()
		{
            pathOptions.Application = "";
            pathOptions.Roms = @"\roms\";
            pathOptions.Saves = @"\saves\";
            pathOptions.Programs =  @"\programs\";

			romOptions.Current48kROM = @"48k.rom";
			//romOptions.Current48kROM = @"seBasic321.rom";

			renderOptions.BorderSize = 0;
            renderOptions.FullScreenMode = false;
            renderOptions.MaintainAspectRatioInFullScreen = true;
            renderOptions.Palette = "Normal";
            renderOptions.PixelSmoothing = false;
            renderOptions.Scanlines = false;
            renderOptions.Vsync = false;
            renderOptions.UseDirectX = false; //ZXIT - Base Window size
            renderOptions.WindowSize = 100; //200%

            audioOptions.EnableAYFor48K = true; //ZXIT
            audioOptions.Mute = false;
            audioOptions.StereoSoundMode = 1;
            audioOptions.Volume = 50;

            emulationOptions.ConfirmOnExit = true;
            emulationOptions.EmulationSpeed = 1;
			emulationOptions.CPUMultiplier = LokeyGlobals.lokey_cpu_multiplier;
            emulationOptions.LateTimings = false;
            emulationOptions.PauseOnFocusLost = true;
            emulationOptions.RestorePreviousSessionOnStart = false;
            emulationOptions.Use128keForSnapshots = false;
            emulationOptions.UseIssue2Keyboard = false;
            emulationOptions.CurrentModelName = "ZX Spectrum 48k";
            emulationOptions.CurrentModel = MachineModel._48k;

            inputDeviceOptions = new InputDeviceSettings();
            inputDeviceOptions.MouseSensitivity = 3;
            inputDeviceOptions.KempstonUsesPort1F = true;
            fileAssociationOptions = new FileAssociationSettings();
        }

        #endregion properties
        public void Load(string path)
		{
			//if (!System.IO.File.Exists(path + "\\zero_config.json"))
			if (true)
			{
                Default();
                return;
            }
            //string json = System.IO.File.ReadAllText(path + "\\zero_config.json");
            //ZeroConfig cfg = JsonConvert.DeserializeObject<ZeroConfig>(json);
            //this.audioOptions = cfg.audioOptions;
            //this.emulationOptions = cfg.emulationOptions;
            //this.fileAssociationOptions = cfg.fileAssociationOptions;
            //this.inputDeviceOptions = cfg.inputDeviceOptions;
            //this.pathOptions = cfg.pathOptions;
            //this.renderOptions = cfg.renderOptions;
            //this.romOptions = cfg.romOptions;
            //this.tapeOptions = cfg.tapeOptions;
            //this.recentFiles = cfg.recentFiles;
        }
        public void Save(string path) {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            System.IO.File.WriteAllText(path + "\\zero_config.json", json);
        }
    }
}