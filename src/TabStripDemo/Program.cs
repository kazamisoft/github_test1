using System;
using System.Drawing;
using System.Windows.Forms;

namespace TabStripDemo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // DPI 설정은 App.config(DpiAwareness=PerMonitorV2) + app.manifest(Windows 10 호환) 에서 한다.
            Application.EnableVisualStyles(); // CueTextBox 안내 문구(EM_SETCUEBANNER)에 필요
            Application.SetCompatibleTextRenderingDefault(false);
            DevExpress.XtraEditors.WindowsFormsSettings.DefaultFont = new Font("Segoe UI", 9f);
            Application.Run(new MainForm());
        }
    }
}
