using PPTBoard.Core.Models;

namespace PPTBoard.Core.Contracts;

/// <summary>放映控制的抽象。WPS 实现，App 只依赖这个接口，不允许直接碰 COM。</summary>
public interface ISlideshowBridge
{
    bool IsAvailable { get; }
    bool IsShowRunning { get; }
    int CurrentPosition { get; }
    int SlideCount { get; }

    bool CanNavigate { get; }
    void Next();
    void Previous();
    void GoTo(int position);
    void ExitShow();

    bool NativePenSupported { get; }
    void SetNativePen(bool enabled);
    void SetNativePointer(int pointerType);
    void SetNativePenColor(InkColor color);
}
