using System;
using System.Windows.Forms;

namespace httpBackupTray;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}
