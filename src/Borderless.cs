using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BorderlessWindowUtility
{
    internal static class Paths
    {
        // Kept in AppData so profiles and settings survive moving or updating the .exe
        public static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BorderlessWindowUtility");
        public static readonly string ProfileFile = Path.Combine(DataDir, "profiles.ini");
        public static readonly string SettingsFile = Path.Combine(DataDir, "settings.ini");
        public static readonly string LogFile = Path.Combine(DataDir, "auto_borderless.log");

        public static string ExePath => Process.GetCurrentProcess().MainModule.FileName;

        static Paths()
        {
            Directory.CreateDirectory(DataDir);
        }
    }

    internal sealed class Profile
    {
        public string Name;
        public string Title;
        public string Process;
        // Empty x means "place by Align", empty y means "center vertically"
        public string X;
        public string Y;
        public string Width;
        public string Height;
        public string Monitor;
        public string Align;
        public bool Auto;
        public double Delay;
    }

    internal static class Profiles
    {
        public static readonly string[] Alignments = { "left", "center", "right" };

        private const string Template =
@"; One section per game. Uncomment and fill in the example below,
; or use ""Window list / add profile"" from the tray icon.
;
; process = executable name (most reliable; check it in Task Manager > Details)
; title   = text contained in the window title (defaults to the section name when process is not set)
; width / height = window size
; x / y   = position; leave them out to center on the monitor (32:9 5120x1440 -> 1280,0)
; align   = left, center (default) or right; where to place the window when x is not set
; monitor = primary (default) or window (the monitor the game opened on)
; delay   = seconds to wait after the window appears before applying (default 3)
; auto    = false to skip this profile in automatic mode

; [My Game]
; process=MyGame.exe
; width=2560
; height=1440
";

        public static void EnsureFile()
        {
            if (!File.Exists(Paths.ProfileFile))
                File.WriteAllText(Paths.ProfileFile, Template.Replace("\r\n", "\n"), new UTF8Encoding(false));
        }

        public static List<Profile> Read()
        {
            var profiles = new List<Profile>();
            foreach (var section in IniFile.Load(Paths.ProfileFile).Sections)
            {
                var c = section.Value;
                string align = IniFile.GetString(c, "align", "center").ToLowerInvariant();
                profiles.Add(new Profile
                {
                    Name = section.Key,
                    Title = IniFile.GetString(c, "title", c.ContainsKey("process") ? "" : section.Key),
                    Process = IniFile.GetString(c, "process", "").ToLowerInvariant(),
                    X = IniFile.GetString(c, "x", ""),
                    Y = IniFile.GetString(c, "y", ""),
                    Width = IniFile.GetString(c, "width", ""),
                    Height = IniFile.GetString(c, "height", ""),
                    Monitor = IniFile.GetString(c, "monitor", "primary").ToLowerInvariant(),
                    Align = Array.IndexOf(Alignments, align) >= 0 ? align : "center",
                    Auto = IniFile.GetBool(c, "auto", true),
                    Delay = IniFile.GetDouble(c, "delay", 3.0),
                });
            }
            return profiles;
        }

        public static Profile Find(List<Profile> profiles, string title, string processName)
        {
            foreach (var profile in profiles)
            {
                if (profile.Process.Length > 0)
                {
                    if (processName == null || profile.Process != processName)
                        continue;
                    if (profile.Title.Length > 0 && !title.Contains(profile.Title))
                        continue;
                    return profile;
                }
                if (profile.Title.Length > 0 && title.Contains(profile.Title))
                    return profile;
            }
            return null;
        }

        public static void Append(string name, string process, string title, int width, int height, string align)
        {
            var lines = new List<string> { $"[{name}]" };
            if (process.Length > 0)
                lines.Add($"process={process}");
            if (title.Length > 0)
                lines.Add($"title={title}");
            lines.Add($"width={width}");
            lines.Add($"height={height}");
            lines.Add($"align={align}");

            string prefix = "";
            if (File.Exists(Paths.ProfileFile))
            {
                string content = File.ReadAllText(Paths.ProfileFile, Encoding.UTF8);
                if (content.Length > 0)
                    prefix = content.EndsWith("\n") ? "\n" : "\n\n";
            }
            File.AppendAllText(Paths.ProfileFile, prefix + string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        }

        /// <summary>Saves the alignment of a profile. A fixed x is removed, since it would override the alignment.</summary>
        public static void SetAlign(string name, string align)
        {
            IniFile.SetValue(Paths.ProfileFile, name, "align", align);
            IniFile.SetValue(Paths.ProfileFile, name, "x", null);
        }
    }

    internal struct WindowRect
    {
        public int X, Y, Width, Height;

        public WindowRect(int x, int y, int width, int height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
    }

    internal static class WindowOps
    {
        /// <summary>Visible top-level windows with a title, as (hwnd, title).</summary>
        public static List<KeyValuePair<IntPtr, string>> FindCandidateWindows()
        {
            var windows = new List<KeyValuePair<IntPtr, string>>();
            Native.EnumWindows((hwnd, _) =>
            {
                if (Native.IsWindowVisible(hwnd) && Native.GetWindow(hwnd, Native.GW_OWNER) == IntPtr.Zero)
                {
                    string title = Native.GetWindowText(hwnd);
                    if (title.Length > 0)
                        windows.Add(new KeyValuePair<IntPtr, string>(hwnd, title));
                }
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public static uint GetProcessId(IntPtr hwnd)
        {
            Native.GetWindowThreadProcessId(hwnd, out uint pid);
            return pid;
        }

        public static string GetProcessName(IntPtr hwnd)
        {
            IntPtr handle = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION, false, GetProcessId(hwnd));
            if (handle == IntPtr.Zero)
                return "";
            try
            {
                var buffer = new StringBuilder(1024);
                int size = buffer.Capacity;
                return Native.QueryFullProcessImageName(handle, 0, buffer, ref size)
                    ? Path.GetFileName(buffer.ToString()).ToLowerInvariant()
                    : "";
            }
            finally
            {
                Native.CloseHandle(handle);
            }
        }

        public static Native.RECT GetMonitorRect(IntPtr hwnd, string monitor)
        {
            IntPtr hMonitor = monitor == "window"
                ? Native.MonitorFromWindow(hwnd, Native.MONITOR_DEFAULTTONEAREST)
                : Native.MonitorFromPoint(new Native.POINT(), Native.MONITOR_DEFAULTTOPRIMARY);
            return Native.GetMonitorRect(hMonitor);
        }

        public static Native.RECT GetPrimaryMonitorRect() => GetMonitorRect(IntPtr.Zero, "primary");

        /// <summary>Position and size for the profile: x follows Align and y is centered when they are not set.</summary>
        public static WindowRect ResolveRect(IntPtr hwnd, Profile profile)
        {
            int width = int.Parse(profile.Width);
            int height = int.Parse(profile.Height);
            var m = GetMonitorRect(hwnd, profile.Monitor);
            int x;
            if (profile.X.Length > 0)
                x = int.Parse(profile.X);
            else if (profile.Align == "left")
                x = m.Left;
            else if (profile.Align == "right")
                x = m.Right - width;
            else
                x = m.Left + (m.Right - m.Left - width) / 2;
            int y = profile.Y.Length == 0 ? m.Top + (m.Bottom - m.Top - height) / 2 : int.Parse(profile.Y);
            return new WindowRect(x, y, width, height);
        }

        public static void MakeBorderless(IntPtr hwnd, WindowRect r)
        {
            Native.SetWindowLong(hwnd, Native.GWL_STYLE, Native.WS_VISIBLE | Native.WS_CLIPCHILDREN);
            Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, 0);
            // Resize 1px smaller first so the application is forced to handle a size change
            Native.MoveWindow(hwnd, r.X, r.Y, r.Width - 1, r.Height - 1, true);
            Native.SetWindowPos(hwnd, IntPtr.Zero, r.X, r.Y, r.Width, r.Height,
                                Native.SWP_FRAMECHANGED | Native.SWP_NOZORDER | Native.SWP_NOOWNERZORDER);
        }

        public static bool IsBorderlessAt(IntPtr hwnd, WindowRect r)
        {
            long style = Native.GetWindowLong(hwnd, Native.GWL_STYLE);
            if ((style & (Native.WS_CAPTION | Native.WS_THICKFRAME)) != 0)
                return false;
            var rect = Native.GetWindowRect(hwnd);
            return rect.Left == r.X && rect.Top == r.Y && rect.Right == r.X + r.Width && rect.Bottom == r.Y + r.Height;
        }

        public static void BringToFront(IntPtr hwnd)
        {
            if (Native.IsIconic(hwnd))
                Native.ShowWindow(hwnd, Native.SW_RESTORE);
            Native.SetForegroundWindow(hwnd);
        }
    }
}
