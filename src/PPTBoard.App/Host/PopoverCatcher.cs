using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PPTBoard.Core.Geometry;

namespace PPTBoard.App.Host;

/// <summary>颜色面板打开时铺满屏幕的透明层：点面板外任意位置关闭面板；主 dock 区域挖空，颜色点仍可点。</summary>
public sealed class PopoverCatcher : OverlayWindow
{
    public event EventHandler? Pressed;

    public PopoverCatcher(LayoutSpec layout)
    {
        Left = 0;
        Top = 0;
        Width = layout.ScreenWidth;
        Height = layout.ScreenHeight;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        Focusable = false;
        MouseLeftButtonDown += (_, _) => Pressed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>把某个窗口（主 dock）从点击层里挖掉，让它继续可点。</summary>
    public void ExcludeWindow(IntPtr hwnd)
    {
        var handle = Handle;
        if (handle == IntPtr.Zero || hwnd == IntPtr.Zero) return;
        if (!GetWindowRect(handle, out var full) || !GetWindowRect(hwnd, out var dock)) return;

        IntPtr region = CreateRectRgn(0, 0, full.Right - full.Left, full.Bottom - full.Top);
        IntPtr hole = CreateRectRgn(
            dock.Left - full.Left,
            dock.Top - full.Top,
            dock.Right - full.Left,
            dock.Bottom - full.Top);

        CombineRgn(region, region, hole, RGN_DIFF);
        SetWindowRgn(handle, region, true);
        DeleteObject(hole);
    }

    private const int RGN_DIFF = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);
}
