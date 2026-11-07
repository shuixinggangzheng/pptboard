using PPTBoard.Core.Contracts;
using PPTBoard.Core.Models;

namespace PPTBoard.Wps;

/// <summary>
/// 连到正在运行的 WPS 演示：优先 GetActiveObject("KWPP.Application")，
/// 失败则从 ROT 里找已打开的演示文稿。所有 COM 调用都包在 try/catch 里，
/// 失败时返回安全值，白板核心不受影响。
/// </summary>
public sealed class WpsBridge : ISlideshowBridge
{
    private const string ProgId = "KWPP.Application";
    private static readonly TimeSpan AttachRetryInterval = TimeSpan.FromMilliseconds(1500);

    private object? _app;
    private DateTime _lastAttachAttempt = DateTime.MinValue;

    /// <summary>连接成功（即便当前没有放映）即为 true。</summary>
    public bool IsAvailable => _app is not null;

    public bool IsShowRunning
    {
        get
        {
            if (_app is null && !TryAttach()) return false;
            try
            {
                dynamic app = _app!;
                return (int)app.SlideShowWindows.Count > 0;
            }
            catch
            {
                Detach();
                return false;
            }
        }
    }

    public int CurrentPosition
    {
        get
        {
            var view = TryGetView();
            if (view is null) return 0;
            try { return (int)view.CurrentShowPosition; }
            catch { return 0; }
        }
    }

    public int SlideCount
    {
        get
        {
            if (_app is null) return 0;
            try
            {
                dynamic app = _app;
                return (int)app.ActivePresentation.Slides.Count;
            }
            catch { return 0; }
        }
    }

    public bool CanNavigate => TryGetView() is not null;

    public bool NativePenSupported
    {
        get
        {
            var view = TryGetView();
            if (view is null) return false;
            try { _ = (int)view.PointerType; return true; }
            catch { return false; }
        }
    }

    public bool TryAttach()
    {
        if (_app is not null) return true;

        var now = DateTime.UtcNow;
        if ((now - _lastAttachAttempt) < AttachRetryInterval) return false;
        _lastAttachAttempt = now;

        try
        {
            object? candidate = ComInterop.TryGetActiveObject(ProgId);
            bool fromPresentation = false;

            if (candidate is null)
            {
                candidate = ComInterop.TryGetPresentationFromRot();
                fromPresentation = candidate is not null;
            }

            if (candidate is null) return false;

            if (fromPresentation)
            {
                dynamic presentation = candidate;
                _app = presentation.Application;
            }
            else
            {
                _app = candidate;
            }

            // 验证一下对象真的可用
            dynamic app = _app!;
            _ = (int)app.SlideShowWindows.Count;
            return true;
        }
        catch
        {
            Detach();
            return false;
        }
    }

    public void Next() => InvokeView(view => view.Next());

    public void Previous() => InvokeView(view => view.Previous());

    public void GoTo(int position) => InvokeView(view => view.GotoSlide(position));

    public void ExitShow() => InvokeView(view => view.Exit());

    public void SetNativePen(bool enabled) => SetNativePointer(enabled ? 2 : 1);

    public void SetNativePointer(int pointerType) => InvokeView(view => view.PointerType = pointerType);

    public void SetNativePenColor(InkColor color)
    {
        // WPS/Office 的 RGB 是 BGR 顺序
        uint bgr = (uint)(color.R | (color.G << 8) | (color.B << 16));
        InvokeView(view => view.PointerColor.RGB = bgr);
    }

    public void Detach()
    {
        _app = null;
    }

    private dynamic? TryGetView()
    {
        if (_app is null && !TryAttach()) return null;
        try
        {
            dynamic app = _app!;
            var windows = app.SlideShowWindows;
            if ((int)windows.Count <= 0) return null;
            return windows.Item(1).View;
        }
        catch
        {
            Detach();
            return null;
        }
    }

    private void InvokeView(Action<dynamic> action)
    {
        var view = TryGetView();
        if (view is null) return;
        try { action(view); }
        catch { /* 放映可能刚好结束 */ }
    }
}
