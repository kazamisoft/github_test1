namespace TabStripDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        DevExpress.XtraEditors.WindowsFormsSettings.DefaultFont = new Font("Segoe UI", 9f);
        Application.Run(new MainForm());
    }
}
