using PPTBoard.Core.Contracts;
using PPTBoard.Core.Models;

namespace PPTBoard.App.Host;

/// <summary>没有 WPS COM 时的空实现：白板可用，放映按钮全部降级。</summary>
public sealed class NullSlideshowBridge : ISlideshowBridge
{
    public bool IsAvailable => false;
    public bool IsShowRunning => false;
    public int CurrentPosition => 0;
    public int SlideCount => 0;
    public bool CanNavigate => false;
    public bool NativePenSupported => false;

    public void Next() { }
    public void Previous() { }
    public void GoTo(int position) { }
    public void ExitShow() { }
    public void SetNativePen(bool enabled) { }
    public void SetNativePointer(int pointerType) { }
    public void SetNativePenColor(InkColor color) { }
}
