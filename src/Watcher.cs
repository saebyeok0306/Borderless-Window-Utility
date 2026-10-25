using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BorderlessWindowUtility
{
    /// <summary>Applies profiles to matching windows; Tick() runs once per second from the tray timer.</summary>
    internal sealed class Watcher
    {
        // Stop fighting a window that keeps undoing our changes (e.g. switched to exclusive fullscreen)
        private const int MaxApplyAttempts = 10;

        public List<Profile> Profiles = new List<Profile>();
        public bool Paused;
        public string LastApplied = "";

        private DateTime? profilesMtime;
        private bool profilesLoaded;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Dictionary<IntPtr, string> processNames = new Dictionary<IntPtr, string>();
        private readonly Dictionary<IntPtr, double> firstSeen = new Dictionary<IntPtr, double>();
        private readonly Dictionary<IntPtr, int> attempts = new Dictionary<IntPtr, int>();

        private void ReloadProfiles()
        {
            DateTime? mtime = File.Exists(Paths.ProfileFile) ? File.GetLastWriteTimeUtc(Paths.ProfileFile) : (DateTime?)null;
            if (profilesLoaded && mtime == profilesMtime)
                return;
            profilesLoaded = true;
            profilesMtime = mtime;
            Profiles = BorderlessWindowUtility.Profiles.Read().Where(p => p.Auto).ToList();
            attempts.Clear();
            Log.Info("Loaded profiles: " + (Profiles.Count > 0 ? string.Join(", ", Profiles.Select(p => p.Name)) : "(none)"));
        }

        /// <summary>Profiles whose game window is open right now.</summary>
        public List<Profile> RunningProfiles()
        {
            var running = new List<Profile>();
            foreach (var window in WindowOps.FindCandidateWindows())
            {
                if (!processNames.TryGetValue(window.Key, out string processName))
                    processName = WindowOps.GetProcessName(window.Key);
                var profile = BorderlessWindowUtility.Profiles.Find(Profiles, window.Value, processName);
                if (profile != null && !running.Contains(profile))
                    running.Add(profile);
            }
            return running;
        }

        public void Tick()
        {
            ReloadProfiles();
            if (Paused)
                return;
            double now = clock.Elapsed.TotalSeconds;
            var alive = new HashSet<IntPtr>();

            foreach (var window in WindowOps.FindCandidateWindows())
            {
                IntPtr hwnd = window.Key;
                string title = window.Value;
                alive.Add(hwnd);
                if (!processNames.TryGetValue(hwnd, out string processName))
                    processNames[hwnd] = processName = WindowOps.GetProcessName(hwnd);
                var profile = BorderlessWindowUtility.Profiles.Find(Profiles, title, processName);
                if (profile == null || Native.IsIconic(hwnd))
                    continue;

                // Give the game time to finish creating/resizing its window before we touch it
                if (!firstSeen.TryGetValue(hwnd, out double seen))
                    firstSeen[hwnd] = seen = now;
                if (now - seen < profile.Delay)
                    continue;

                var rect = WindowOps.ResolveRect(hwnd, profile);
                if (WindowOps.IsBorderlessAt(hwnd, rect))
                    continue;
                attempts.TryGetValue(hwnd, out int count);
                if (count >= MaxApplyAttempts)
                    continue;
                attempts[hwnd] = count + 1;

                WindowOps.MakeBorderless(hwnd, rect);
                if (WindowOps.IsBorderlessAt(hwnd, rect))
                {
                    LastApplied = $"{profile.Name}: {rect.Width}x{rect.Height} ({rect.X}, {rect.Y})";
                    Log.Info($"Applied \"{profile.Name}\" to \"{title}\" ({processName}): " +
                             $"{rect.Width}x{rect.Height} at {rect.X},{rect.Y}");
                }
                else
                {
                    Log.Warning($"Could not apply \"{profile.Name}\" to \"{title}\" (attempt {count + 1}/{MaxApplyAttempts}). " +
                                "If the game runs as administrator, install with --elevated. " +
                                "If it is in exclusive fullscreen, switch it to windowed mode.");
                }
            }

            foreach (var hwnd in processNames.Keys.Where(h => !alive.Contains(h)).ToList())
                processNames.Remove(hwnd);
            foreach (var hwnd in firstSeen.Keys.Where(h => !alive.Contains(h)).ToList())
                firstSeen.Remove(hwnd);
            foreach (var hwnd in attempts.Keys.Where(h => !alive.Contains(h)).ToList())
                attempts.Remove(hwnd);
        }
    }
}
