using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BorderlessWindowUtility
{
    /// <summary>
    /// Reads .ini files the way the Python version's configparser does: case-sensitive section names,
    /// case-insensitive keys, "=" or ":" separators, and ";" / "#" comments (inline ones need a space before them).
    /// </summary>
    internal sealed class IniFile
    {
        public readonly List<KeyValuePair<string, Dictionary<string, string>>> Sections =
            new List<KeyValuePair<string, Dictionary<string, string>>>();

        public static IniFile Load(string path)
        {
            var ini = new IniFile();
            if (!File.Exists(path))
                return ini;

            Dictionary<string, string> current = null;
            foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = StripInlineComment(rawLine).Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                    continue;

                if (line[0] == '[' && line.EndsWith("]"))
                {
                    string name = line.Substring(1, line.Length - 2);
                    current = ini.Get(name);
                    if (current == null)
                    {
                        current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        ini.Sections.Add(new KeyValuePair<string, Dictionary<string, string>>(name, current));
                    }
                    continue;
                }

                int separator = line.IndexOfAny(new[] { '=', ':' });
                if (current == null || separator <= 0)
                    continue;
                current[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }
            return ini;
        }

        public Dictionary<string, string> Get(string section)
        {
            foreach (var pair in Sections)
                if (pair.Key == section)
                    return pair.Value;
            return null;
        }

        /// <summary>
        /// Sets (or with a null value, removes) a key in one section of the file,
        /// keeping every other line and comment as it is.
        /// </summary>
        public static void SetValue(string path, string section, string key, string value)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8).ToList();
            string Clean(int i) => StripInlineComment(lines[i]).Trim();
            bool IsHeader(string line) => line.StartsWith("[") && line.EndsWith("]");

            int start = lines.FindIndex(line => StripInlineComment(line).Trim() == $"[{section}]");
            if (start < 0)
                return;
            int end = start + 1;
            while (end < lines.Count && !IsHeader(Clean(end)))
                end++;

            var matches = new List<int>();
            int lastContent = start;
            for (int i = start + 1; i < end; i++)
            {
                string line = Clean(i);
                if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                    continue;
                lastContent = i;
                int separator = line.IndexOfAny(new[] { '=', ':' });
                if (separator > 0 && string.Equals(line.Substring(0, separator).Trim(), key, StringComparison.OrdinalIgnoreCase))
                    matches.Add(i);
            }

            if (value != null && matches.Count > 0)
                lines[matches[0]] = $"{key}={value}";
            else if (value != null)
                lines.Insert(lastContent + 1, $"{key}={value}");
            for (int i = matches.Count - 1; i >= (value != null ? 1 : 0); i--)
                lines.RemoveAt(matches[i]);

            File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        }

        private static string StripInlineComment(string line)
        {
            for (int i = 1; i < line.Length; i++)
                if ((line[i] == ';' || line[i] == '#') && char.IsWhiteSpace(line[i - 1]))
                    return line.Substring(0, i);
            return line;
        }

        public static string GetString(Dictionary<string, string> section, string key, string fallback) =>
            section.TryGetValue(key, out string value) ? value : fallback;

        public static bool GetBool(Dictionary<string, string> section, string key, bool fallback)
        {
            if (!section.TryGetValue(key, out string value))
                return fallback;
            switch (value.ToLowerInvariant())
            {
                case "1": case "yes": case "true": case "on": return true;
                case "0": case "no": case "false": case "off": return false;
                default: return fallback;
            }
        }

        public static double GetDouble(Dictionary<string, string> section, string key, double fallback) =>
            section.TryGetValue(key, out string value) &&
            double.TryParse(value, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double result)
                ? result
                : fallback;
    }
}
