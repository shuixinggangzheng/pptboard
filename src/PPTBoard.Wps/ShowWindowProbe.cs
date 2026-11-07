using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PPTBoard.Wps;

/// <summary>COM 不可用时的回退：找 wpp.exe / POWERPNT.EXE 的全屏放映窗口。</summary>
public static class ShowWindowProbe
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public static bool IsRunning()
    {
        int screenWidth = GetSystemMetrics(0);
        int screenHeight = GetSystemMetrics(1);
        bool found = false;

        // 优先按进程名认 wpp.exe / POWERPNT.EXE 的全屏窗口，避免类名/标题随版本变化
        var officePids = new HashSet<uint>();
        foreach (var processName in new[] { "wpp", "POWERPNT" })
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    officePids.Add((uint)process.Id);
                    process.Dispose();
                }
            }
            catch
            {
                // 进程可能刚好退出
            }
        }

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;

            GetWindowThreadProcessId(hWnd, out uint pid);
            bool officeWindow = officePids.Contains(pid);

            var className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            string cls = className.ToString();

            var title = new StringBuilder(512);
            GetWindowText(hWnd, title, title.Capacity);
            string text = title.ToString();

            bool wpsShow = cls == "Qt5QWindowIcon" && text.StartsWith("WPS Presentation Slide Show", StringComparison.OrdinalIgnoreCase);
            bool pptShow = cls == "screenClass";

            if (!officeWindow && !wpsShow && !pptShow) return true;

            if (GetWindowRect(hWnd, out var rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width >= screenWidth * 0.9 && height >= screenHeight * 0.9)
                {
                    found = true;
                    return false;
                }
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }
}
