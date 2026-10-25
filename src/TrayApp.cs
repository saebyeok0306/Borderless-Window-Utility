using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BorderlessWindowUtility
{
    /// <summary>
    /// Tray icon and menu. Uses Shell_NotifyIcon and a native popup menu directly (instead of NotifyIcon)
    /// so the menu looks like the rest of Windows and every item is reachable with a WM_COMMAND id.
    /// </summary>
    internal sealed class TrayApp : NativeWindow, IDisposable
    {
        public const string WindowTitle = "BorderlessWindowUtilityTray";
        public const int IdWindowList = 1001, IdProfiles = 1003, IdLog = 1004,
                         IdPause = 1005, IdExit = 1006, IdStartup = 1007, IdLanguageBase = 1101, IdAlignBase = 1201;
        private const int WmTray = Native.WM_USER + 20;
        private static readonly string[] LanguageOptions = new[] { "auto" }.Concat(I18n.Languages.Keys).ToArray();

        private readonly Watcher watcher = new Watcher();
        private readonly Timer timer = new Timer { Interval = 1000 };
        private readonly int taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
        private readonly Icon icon;
        private bool iconAdded;
        private string tooltip = "";
        private WindowListForm windowList;
        // Menu id -> (profile, alignment) for the Alignment submenu, rebuilt each time the menu opens
        private readonly Dictionary<int, (string Profile, string Align)> alignCommands =
            new Dictionary<int, (string Profile, string Align)>();

        public TrayApp()
        {
            // A hidden top-level window (not message-only) so it receives the TaskbarCreated broadcast
            CreateHandle(new CreateParams { Caption = WindowTitle });

            using (var stream = typeof(TrayApp).Assembly.GetManifestResourceStream("icon.ico"))
                icon = new Icon(stream, SystemInformation.SmallIconSize);

            UpdateIcon();
            timer.Tick += (s, e) => OnTimer();
            timer.Start();

            // Keep the logon entry pointing at this .exe if it was moved
            string entry = Startup.GetRunEntry();
            if (entry != null && entry != Startup.Command)
            {
                Startup.Install(elevated: false, quiet: true);
                Log.Info("Updated startup entry to " + Startup.Command);
            }
        }

        private string StatusText()
        {
            if (watcher.Paused)
                return I18n.Tr("status_paused");
            int count = watcher.Profiles.Count;
            return count == 1 ? I18n.Tr("status_watching_one") : I18n.Tr("status_watching", "count", count.ToString());
        }

        private void UpdateIcon()
        {
            string text = $"{Startup.DisplayName}\n{StatusText()}";
            if (text.Length > 127)
                text = text.Substring(0, 127);
            if (iconAdded && text == tooltip)
                return;
            var data = new Native.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(Native.NOTIFYICONDATA)),
                hWnd = Handle,
                uFlags = Native.NIF_ICON | Native.NIF_MESSAGE | Native.NIF_TIP,
                uCallbackMessage = WmTray,
                hIcon = icon.Handle,
                szTip = text,
            };
            // Fails while Explorer is not ready yet (early at logon) or restarting; retried on the next tick
            iconAdded = Native.Shell_NotifyIcon(iconAdded ? Native.NIM_MODIFY : Native.NIM_ADD, ref data);
            if (iconAdded)
                tooltip = text;
        }

        private void OnTimer()
        {
            try
            {
                watcher.Tick();
            }
            catch (Exception ex)
            {
                Log.Error("Error while checking windows", ex);
            }
            UpdateIcon();
        }

        private void ShowMenu()
        {
            IntPtr menu = Native.CreatePopupMenu();
            void Add(uint flags, int id, string text) => Native.AppendMenu(menu, flags, (IntPtr)id, text);

            Add(Native.MF_STRING | Native.MF_GRAYED, 0, StatusText());
            if (watcher.LastApplied.Length > 0)
                Add(Native.MF_STRING | Native.MF_GRAYED, 0, I18n.Tr("last_applied", "text", watcher.LastApplied.Replace("&", "&&")));
            Add(Native.MF_SEPARATOR, 0, null);
            Add(Native.MF_STRING, IdWindowList, I18n.Tr("menu_window_list"));
            Add(Native.MF_STRING, IdProfiles, I18n.Tr("menu_profiles"));
            Add(Native.MF_STRING, IdLog, I18n.Tr("menu_log"));
            Add(Native.MF_SEPARATOR, 0, null);

            Native.AppendMenu(menu, Native.MF_POPUP, CreateAlignMenu(), I18n.Tr("menu_align"));

            IntPtr languageMenu = Native.CreatePopupMenu();
            string setting = I18n.ReadSetting();
            for (int i = 0; i < LanguageOptions.Length; i++)
            {
                string language = LanguageOptions[i];
                string label = language == "auto" ? I18n.Tr("menu_language_auto") : I18n.Languages[language];
                Native.AppendMenu(languageMenu, Native.MF_STRING | (language == setting ? Native.MF_CHECKED : 0),
                                  (IntPtr)(IdLanguageBase + i), label);
            }
            Native.AppendMenu(menu, Native.MF_POPUP, languageMenu, I18n.Tr("menu_language"));
            Add(Native.MF_STRING | (Startup.IsEnabled() ? Native.MF_CHECKED : 0), IdStartup, I18n.Tr("menu_startup"));
            Add(Native.MF_STRING | (watcher.Paused ? Native.MF_CHECKED : 0), IdPause, I18n.Tr("menu_pause"));
            Add(Native.MF_SEPARATOR, 0, null);
            Add(Native.MF_STRING, IdExit, I18n.Tr("menu_exit"));
            Native.SetMenuDefaultItem(menu, IdWindowList, 0);

            Native.GetCursorPos(out var pos);
            // Required so the menu closes when clicking elsewhere
            Native.SetForegroundWindow(Handle);
            Native.TrackPopupMenu(menu, Native.TPM_RIGHTBUTTON, pos.X, pos.Y, 0, Handle, IntPtr.Zero);
            Native.PostMessage(Handle, Native.WM_NULL, IntPtr.Zero, IntPtr.Zero);
            Native.DestroyMenu(menu);
        }

        /// <summary>Left / Center / Right for the running game, or one submenu per game when several are running.</summary>
        private IntPtr CreateAlignMenu()
        {
            IntPtr alignMenu = Native.CreatePopupMenu();
            alignCommands.Clear();
            var running = watcher.RunningProfiles();
            if (running.Count == 0)
                Native.AppendMenu(alignMenu, Native.MF_STRING | Native.MF_GRAYED, IntPtr.Zero, I18n.Tr("menu_align_none"));

            for (int p = 0; p < running.Count; p++)
            {
                var profile = running[p];
                string name = profile.Name.Replace("&", "&&");
                IntPtr target = alignMenu;
                if (running.Count > 1)
                {
                    target = Native.CreatePopupMenu();
                    Native.AppendMenu(alignMenu, Native.MF_POPUP, target, name);
                }
                else
                {
                    Native.AppendMenu(alignMenu, Native.MF_STRING | Native.MF_GRAYED, IntPtr.Zero, name);
                    Native.AppendMenu(alignMenu, Native.MF_SEPARATOR, IntPtr.Zero, null);
                }

                for (int a = 0; a < Profiles.Alignments.Length; a++)
                {
                    string align = Profiles.Alignments[a];
                    int id = IdAlignBase + p * Profiles.Alignments.Length + a;
                    alignCommands[id] = (profile.Name, align);
                    // A fixed x overrides the alignment, so nothing is checked then
                    bool current = profile.X.Length == 0 && profile.Align == align;
                    Native.AppendMenu(target, Native.MF_STRING | (current ? Native.MF_CHECKED : 0), (IntPtr)id,
                                      I18n.Tr("align_" + align));
                }
            }
            return alignMenu;
        }

        private void OnCommand(int command)
        {
            if (alignCommands.TryGetValue(command, out var alignCommand))
            {
                Profiles.SetAlign(alignCommand.Profile, alignCommand.Align);
                Log.Info($"Alignment of \"{alignCommand.Profile}\" set to {alignCommand.Align}");
                // Move the window now instead of on the next timer tick
                watcher.Tick();
                return;
            }

            switch (command)
            {
                case IdWindowList:
                    if (windowList == null || windowList.IsDisposed)
                    {
                        windowList = new WindowListForm();
                        windowList.Show();
                    }
                    windowList.Activate();
                    WindowOps.BringToFront(windowList.Handle);
                    break;
                case IdProfiles:
                    Profiles.EnsureFile();
                    OpenFile(Paths.ProfileFile);
                    break;
                case IdLog:
                    OpenFile(Paths.LogFile);
                    break;
                case IdPause:
                    watcher.Paused = !watcher.Paused;
                    Log.Info("Auto-apply " + (watcher.Paused ? "paused" : "resumed"));
                    UpdateIcon();
                    break;
                case IdStartup:
                    if (Startup.IsEnabled())
                        Startup.Uninstall(quiet: true);
                    else
                        Startup.Install(elevated: false, quiet: true);
                    Log.Info("Start with Windows: " + (Startup.IsEnabled() ? "on" : "off"));
                    break;
                case IdExit:
                    Application.ExitThread();
                    break;
                default:
                    int index = command - IdLanguageBase;
                    if (index >= 0 && index < LanguageOptions.Length)
                    {
                        I18n.WriteSetting(LanguageOptions[index]);
                        Log.Info("Language set to " + LanguageOptions[index]);
                        UpdateIcon();
                    }
                    break;
            }
        }

        private static void OpenFile(string path)
        {
            if (!System.IO.File.Exists(path))
                System.IO.File.WriteAllText(path, "");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == taskbarCreated)
                {
                    iconAdded = false;
                    UpdateIcon();
                }
                else if (m.Msg == WmTray)
                {
                    int mouse = (int)m.LParam;
                    if (mouse == Native.WM_RBUTTONUP)
                        ShowMenu();
                    else if (mouse == Native.WM_LBUTTONDBLCLK)
                        OnCommand(IdWindowList);
                }
                else if (m.Msg == Native.WM_COMMAND)
                {
                    OnCommand((int)m.WParam & 0xFFFF);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error handling tray message", ex);
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            timer.Dispose();
            var data = new Native.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(Native.NOTIFYICONDATA)),
                hWnd = Handle,
            };
            Native.Shell_NotifyIcon(Native.NIM_DELETE, ref data);
            icon.Dispose();
            DestroyHandle();
            Log.Info("Exiting");
        }
    }
}
