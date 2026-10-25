using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BorderlessWindowUtility
{
    /// <summary>Starting at logon: a registry Run entry, or a scheduled task when admin rights are needed.</summary>
    internal static class Startup
    {
        public const string AppName = "BorderlessWindowUtility";
        public const string DisplayName = "Borderless Window Utility";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static string Command => $"\"{Paths.ExePath}\"";

        public static void Report(string message) =>
            MessageBox.Show(message, DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static (int ExitCode, string Output) Schtasks(params string[] args)
        {
            var info = new ProcessStartInfo("schtasks", string.Join(" ", args))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var process = Process.Start(info))
            {
                string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();
                return (process.ExitCode, output.Trim());
            }
        }

        public static string GetRunEntry()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                return key?.GetValue(AppName) as string;
        }

        public static bool HasScheduledTask() => Schtasks("/Query", "/TN", AppName).ExitCode == 0;

        public static bool IsEnabled() => GetRunEntry() != null || HasScheduledTask();

        public static int Install(bool elevated, bool quiet = false)
        {
            if (Uninstall(quiet: true) != 0)
                return 1;
            if (elevated)
            {
                var result = Schtasks("/Create", "/F", "/TN", AppName, "/SC", "ONLOGON", "/RL", "HIGHEST",
                                      "/DELAY", "0000:15", "/TR", $"\"\\\"{Paths.ExePath}\\\"\"");
                if (result.ExitCode != 0)
                {
                    Report(result.Output + "\nFailed. Run this command from an administrator terminal.");
                    return 1;
                }
                if (!quiet)
                    Report($"Registered scheduled task \"{AppName}\" (runs at logon with highest privileges).");
            }
            else
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
                    key.SetValue(AppName, Command);
                if (!quiet)
                    Report($"Registered startup entry: {Command}");
            }
            return 0;
        }

        public static int Uninstall(bool quiet = false)
        {
            if (GetRunEntry() != null)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true))
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                if (!quiet)
                    Report("Removed startup registry entry.");
            }
            if (HasScheduledTask())
            {
                var result = Schtasks("/Delete", "/F", "/TN", AppName);
                if (result.ExitCode != 0)
                {
                    Report(result.Output + $"\nCould not remove the scheduled task \"{AppName}\". " +
                           $"Run this from an administrator terminal:\n{Command} --uninstall");
                    return 1;
                }
                if (!quiet)
                    Report($"Removed scheduled task \"{AppName}\".");
            }
            return 0;
        }
    }
}
