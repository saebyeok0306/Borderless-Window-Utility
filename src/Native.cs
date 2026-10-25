using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessWindowUtility
{
    /// <summary>Win32 functions and constants used by the app.</summary>
    internal static class Native
    {
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const uint GW_OWNER = 4;

        public const long WS_VISIBLE = 0x10000000;
        public const long WS_CLIPCHILDREN = 0x02000000;
        public const long WS_CAPTION = 0x00C00000;
        public const long WS_THICKFRAME = 0x00040000;
        public const long WS_EX_TOOLWINDOW = 0x00000080;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_NOOWNERZORDER = 0x0200;

        public const int SW_RESTORE = 9;
        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const uint MONITOR_DEFAULTTOPRIMARY = 1;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const int DWMWA_CLOAKED = 14;

        public const int WM_NULL = 0x0000;
        public const int WM_DESTROY = 0x0002;
        public const int WM_COMMAND = 0x0111;
        public const int WM_USER = 0x0400;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int WM_RBUTTONUP = 0x0205;

        public const uint MF_STRING = 0x0000;
        public const uint MF_GRAYED = 0x0001;
        public const uint MF_CHECKED = 0x0008;
        public const uint MF_POPUP = 0x0010;
        public const uint MF_SEPARATOR = 0x0800;
        public const uint TPM_RIGHTBUTTON = 0x0002;

        public const uint NIM_ADD = 0;
        public const uint NIM_MODIFY = 1;
        public const uint NIM_DELETE = 2;
        public const uint NIF_MESSAGE = 0x1;
        public const uint NIF_ICON = 0x2;
        public const uint NIF_TIP = 0x4;

        public const int ASFW_ANY = -1;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
            public uint uVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static long GetWindowLong(IntPtr hWnd, int index) =>
            IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, index).ToInt64() : (uint)GetWindowLong32(hWnd, index);

        public static void SetWindowLong(IntPtr hWnd, int index, long value)
        {
            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hWnd, index, new IntPtr(value));
            else
                SetWindowLong32(hWnd, index, unchecked((int)value));
        }

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        public static extern bool SetMenuDefaultItem(IntPtr hMenu, uint uItem, uint fByPos);

        [DllImport("user32.dll")]
        public static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern ushort GetUserDefaultUILanguage();

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        public static string GetWindowText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return string.Empty;
            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetClassName(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static RECT GetWindowRect(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT rect);
            return rect;
        }

        public static RECT GetMonitorRect(IntPtr hMonitor)
        {
            var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            GetMonitorInfo(hMonitor, ref info);
            return info.rcMonitor;
        }
    }
}
