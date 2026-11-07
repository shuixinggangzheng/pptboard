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
    private static readonly string[] ProgIds = { "PowerPoint.Application", "KWPP.Application" };
    private static readonly TimeSpan AttachRetryInterval = TimeSpan.FromMilliseconds(1500);

    private object? _app;
    private DateTime _lastAttachAttempt = DateTime.MinValue;

    public string? LastError { get; private set; }
    public string Diagnostics { get; private set; } = "";

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
        if (_app is not null)
        {
            try
            {
                // 已经在放映就用现成的；没有放映则定期重新扫描，防止连到没有放映的旧实例
                if ((int)((dynamic)_app).SlideShowWindows.Count > 0) return true;
            }
            catch
            {
                Detach();
            }
        }

        var now = DateTime.UtcNow;
        if ((now - _lastAttachAttempt) < AttachRetryInterval) return _app is not null;
        _lastAttachAttempt = now;

        try
        {
            object? best = FindBestApp(out string diagnostics);
            Diagnostics = diagnostics;
            if (best is null)
            {
                LastError = "no WPS automation object found; " + diagnostics;
                return false;
            }

            _app = best;
            dynamic app = _app;
            _ = (int)app.SlideShowWindows.Count;
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            LastError = "attach/verify failed: " + ex.Message;
            Detach();
            return false;
        }
    }

    /// <summary>可能有多个 WPS 实例；优先选正在放映的那个。</summary>
    private static object? FindBestApp(out string diagnostics)
    {
        var candidates = new List<object>();
        var parts = new List<string>();

        foreach (var progId in ProgIds)
        {
            var active = ComInterop.TryGetActiveObject(progId);
            if (active is not null)
            {
                candidates.Add(active);
                parts.Add(progId + ":" + CountShows(active));
            }
            else
            {
                parts.Add(progId + ":null");
            }
        }

        var presentations = ComInterop.GetPresentationsFromRot();
        parts.Add("rotDocs:" + presentations.Count);
        foreach (var presentation in presentations)
        {
            try
            {
                dynamic doc = presentation;
                var app = doc.Application;
                if (app is not null)
                {
                    candidates.Add(app);
                    parts.Add("rot:" + CountShows(app));
                }
            }
            catch
            {
                // 单个文档对象不可用
            }
        }

        object? fallback = null;
        foreach (var candidate in candidates)
        {
            try
            {
                dynamic app = candidate;
                if ((int)app.SlideShowWindows.Count > 0)
                {
                    diagnostics = string.Join(", ", parts) + " => picked show";
                    return candidate;
                }
                fallback ??= candidate;
            }
            catch
            {
                // 跳过不可用实例
            }
        }

        diagnostics = string.Join(", ", parts) + " => no show";
        return fallback;
    }

    private static string CountShows(object app)
    {
        try
        {
            dynamic d = app;
            return ((int)d.SlideShowWindows.Count).ToString();
        }
        catch (Exception ex)
        {
            return "err:" + ex.Message;
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
