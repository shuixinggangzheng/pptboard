using System.Windows;
using PPTBoard.App.Interop;

namespace PPTBoard.App.Host;

/// <summary>按列表顺序（底 → 顶）重排置顶窗口。</summary>
public sealed class WindowStack
{
    private readonly List<OverlayWindow> _windows = new();

    public void Add(OverlayWindow window)
    {
        if (!_windows.Contains(window)) _windows.Add(window);
    }

    public void MoveToTop(OverlayWindow window)
    {
        if (_windows.Remove(window)) _windows.Add(window);
    }

    public void AddFirst(OverlayWindow window)
    {
        _windows.Remove(window);
        _windows.Insert(0, window);
    }

    public void Remove(OverlayWindow window) => _windows.Remove(window);

    public void BringToFront()
    {
        var visible = _windows
            .Where(w => w.IsVisible && w.Handle != IntPtr.Zero)
            .ToList();

        // 列表顺序是“底 → 顶”。实测 SetWindowPos(a, b) 会把 a 放到 b 上面，
        // 所以让 i+1 抬到 i 之上，整条链就按列表顺序排好。
        for (int i = 0; i < visible.Count - 1; i++)
        {
            NativeMethods.SetWindowPos(visible[i + 1].Handle, visible[i].Handle, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }

        if (visible.Count > 0)
        {
            NativeMethods.SetWindowPos(visible[^1].Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }
}
