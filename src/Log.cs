using System;
using System.IO;
using System.Text;

namespace BorderlessWindowUtility
{
    internal static class Log
    {
        public static void Info(string message) => Write("INFO", message);

        public static void Warning(string message) => Write("WARNING", message);

        public static void Error(string message, Exception ex) => Write("ERROR", $"{message}\n{ex}");

        private static void Write(string level, string message)
        {
            try
            {
                File.AppendAllText(Paths.LogFile,
                                   $"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} {level} {message}\n",
                                   new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // Logging must never take the app down
            }
        }
    }
}
