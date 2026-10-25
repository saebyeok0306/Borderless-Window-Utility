using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace BorderlessWindowUtility
{
    /// <summary>
    ///   BorderlessWindowUtility.exe                      run in the tray and apply profiles automatically
    ///   BorderlessWindowUtility.exe --install            run automatically at logon (current user)
    ///   BorderlessWindowUtility.exe --install --elevated run at logon with admin rights (Task Scheduler),
    ///                                                    needed when the game itself runs as administrator
    ///   BorderlessWindowUtility.exe --uninstall          remove automatic startup
    ///   BorderlessWindowUtility.exe --window-list        open the window list
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool Has(string option) => args.Contains(option, StringComparer.OrdinalIgnoreCase);

            if (Has("--install"))
                return Startup.Install(Has("--elevated"));
            if (Has("--uninstall"))
                return Startup.Uninstall();
            if (Has("--window-list"))
            {
                Application.Run(new WindowListForm());
                return 0;
            }

            // Shared with the Python version, so only one of them runs at a time
            using (var mutex = new Mutex(false, @"Local\" + Startup.AppName + "Watcher", out bool created))
            {
                if (!created)
                {
                    // Already running: open its window list instead of starting a second copy
                    IntPtr tray = Native.FindWindow(null, TrayApp.WindowTitle);
                    if (tray == IntPtr.Zero)
                        tray = Native.FindWindow(TrayApp.WindowTitle, null);  // the Python version's tray window class
                    if (tray != IntPtr.Zero)
                    {
                        Native.AllowSetForegroundWindow(Native.ASFW_ANY);
                        Native.PostMessage(tray, Native.WM_COMMAND, (IntPtr)TrayApp.IdWindowList, IntPtr.Zero);
                    }
                    return 0;
                }

                Profiles.EnsureFile();
                Log.Info("Watching for windows (profiles: " + Paths.ProfileFile + ")");
                using (new TrayApp())
                    Application.Run();
            }
            return 0;
        }
    }
}
