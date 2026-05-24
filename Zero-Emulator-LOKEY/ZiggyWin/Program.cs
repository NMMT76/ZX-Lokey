using System.Windows.Forms;

namespace ZeroWin
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [System.STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ZXForm form = new ZXForm();
            Application.Idle += new System.EventHandler(form.OnApplicationIdle);
            Application.Run(form);
        }
    }
}