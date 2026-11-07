using System.Diagnostics;
using PPTBoard.Core.Contracts;
using PPTBoard.Core.Models;

namespace PPTBoard.App.Host;

/// <summary>仅供无窗口测试：0-6s 放映，6-6.6s 抖一下，6.6-9s 恢复，之后结束。</summary>
public sealed class FakeSlideshowBridge : ISlideshowBridge
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public bool IsAvailable => true;
    public bool IsShowRunning
    {
        get
        {
            double seconds = _clock.Elapsed.TotalSeconds;
            return seconds < 6 || (seconds > 6.6 && seconds < 9);
        }
    }

    public int CurrentPosition => 1;
    public int SlideCount => 3;
    public bool CanNavigate => true;
    public bool NativePenSupported => false;

    public void Next() { }
    public void Previous() { }
    public void GoTo(int position) { }
    public void ExitShow() { }
    public void SetNativePen(bool enabled) { }
    public void SetNativePointer(int pointerType) { }
    public void SetNativePenColor(InkColor color) { }
}
