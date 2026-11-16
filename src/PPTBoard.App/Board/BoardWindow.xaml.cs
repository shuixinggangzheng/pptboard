using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PPTBoard.App.Host;
using PPTBoard.App.Interop;
using PPTBoard.Core.Geometry;
using PPTBoard.Ink;

namespace PPTBoard.App.Board;

public partial class BoardWindow : OverlayWindow
{
    private const int MousePointerId = 0x7FFF;
    private const int PenBase = 0x10000;
    private const int TouchBase = 0x20000;

    private readonly InkEngine _engine;
    private readonly HashSet<int> _active = new();
    private bool _mouseDown;
    private bool _eraserArmed;

    public event EventHandler? CollapseRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? PageMenuRequested;

    /// <summary>有弹层（页面菜单/颜色面板/清空滑条）打开时，笔迹输入先让位给“关弹层”。</summary>
    public Func<bool>? IsPopupOpen { get; set; }
    public Action? DismissPopups { get; set; }

    public BoardWindow(LayoutSpec layout, InkEngine engine)
    {
        AllowsTransparency = false;
        Background = Brushes.White;
        InitializeComponent();

        _engine = engine;
        Width = layout.Board.Width;
        Height = layout.Board.Height;
        Left = layout.Board.X;
        Top = -layout.Board.Height;

        PreviewMouseLeftButtonDown += (_, e) =>
            Log.Info($"board mouse preview at {e.GetPosition(this)} source={e.OriginalSource?.GetType().Name}");

        HookInput();
        Ink.Attach(engine);
        engine.Document.CurrentPageChanged += OnPageChanged;
        engine.Document.PagesChanged += OnPageChanged;
        UpdatePageText();
    }

    /// <summary>绘制区高度（行 0）；灰条高度由布局行定义，不再硬编码。</summary>
    public double DrawingHeight => Ink.ActualHeight > 0 ? Ink.ActualHeight : Height - 56;

    public void SetBoardActive(bool active) => EraserButton.Background = Brushes.Transparent;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        Interop.NativeMethods.GetWindowRect(hwnd, out var rect);
        Log.Info($"board hwnd={hwnd} rect=({rect.Left},{rect.Top})-({rect.Right},{rect.Bottom}) size={Width}x{Height}");
    }

    private void HookInput()
    {
        // 笔（WPF Stylus）——坐标已经是窗口客户区 DIP
        StylusDown += OnStylusDown;
        StylusMove += OnStylusMove;
        StylusUp += OnStylusUp;
        LostStylusCapture += OnLostStylusCapture;

        // 手指（WPF Touch）——每根手指一个 TouchDevice
        TouchDown += OnTouchDown;
        TouchMove += OnTouchMove;
        TouchUp += OnTouchUp;
        LostTouchCapture += OnLostTouchCapture;

        LostMouseCapture += OnLostMouseCapture;
    }

    private bool TryDismissPopup()
    {
        if (IsPopupOpen?.Invoke() != true) return false;
        DismissPopups?.Invoke();
        return true;
    }

    private static bool TryLastStylusPoint(StylusEventArgs e, IInputElement relativeTo, out Point point)
    {
        var points = e.GetStylusPoints(relativeTo);
        if (points.Count == 0)
        {
            point = default;
            return false;
        }
        point = new Point(points[points.Count - 1].X, points[points.Count - 1].Y);
        return true;
    }

    private void OnStylusDown(object sender, StylusEventArgs e)
    {
        var device = e.StylusDevice;
        if (device is null) return;
        // 有的驱动会把手指也报成 Stylus；手指统一走 Touch 通道
        if (device.TabletDevice?.Type == TabletDeviceType.Touch) return;

        if (TryDismissPopup())
        {
            e.Handled = true;
            return;
        }

        if (!TryLastStylusPoint(e, this, out var p)) return;
        if (p.Y < 0 || p.Y >= DrawingHeight) return;

        int id = PenBase | (int)device.Id;
        if (!_engine.PointerDown(id, p.X, p.Y)) return;

        _active.Add(id);
        CaptureStylus();
        Log.Info($"board pen down id={id} device={device.Id} at {p.X:0.0},{p.Y:0.0}");
        e.Handled = true;
    }

    private void OnStylusMove(object sender, StylusEventArgs e)
    {
        var device = e.StylusDevice;
        if (device is null) return;
        if (device.TabletDevice?.Type == TabletDeviceType.Touch) return;

        int id = PenBase | (int)device.Id;
        if (!_active.Contains(id)) return;

        foreach (var point in e.GetStylusPoints(this))
            _engine.PointerMove(id, point.X, point.Y);
        e.Handled = true;
    }

    private void OnStylusUp(object sender, StylusEventArgs e)
    {
        var device = e.StylusDevice;
        if (device is null) return;

        int id = PenBase | (int)device.Id;
        if (_active.Contains(id))
        {
            if (TryLastStylusPoint(e, this, out var p))
                _engine.PointerMove(id, p.X, p.Y);
            EndPointer(id, cancel: false);
            Log.Info($"board pen up id={id}");
        }

        ReleaseStylusCapture();
        e.Handled = true;
    }

    private void OnLostStylusCapture(object sender, StylusEventArgs e)
    {
        var device = e.StylusDevice;
        if (device is null) return;
        int id = PenBase | (int)device.Id;
        if (_active.Contains(id))
        {
            EndPointer(id, cancel: true);
            Log.Info($"board pen capture lost id={id}");
        }
    }

    private void OnTouchDown(object? sender, TouchEventArgs e)
    {
        if (TryDismissPopup())
        {
            e.Handled = true;
            return;
        }

        var p = e.GetTouchPoint(this).Position;
        if (p.Y < 0 || p.Y >= DrawingHeight) return;

        int id = TouchBase | (int)e.TouchDevice.Id;
        if (!_engine.PointerDown(id, p.X, p.Y)) return;

        _active.Add(id);
        CaptureTouch(e.TouchDevice);
        Log.Info($"board touch down id={id} device={e.TouchDevice.Id} at {p.X:0.0},{p.Y:0.0}");
        e.Handled = true;
    }

    private void OnTouchMove(object? sender, TouchEventArgs e)
    {
        int id = TouchBase | (int)e.TouchDevice.Id;
        if (!_active.Contains(id)) return;

        var p = e.GetTouchPoint(this).Position;
        _engine.PointerMove(id, p.X, p.Y);
        e.Handled = true;
    }

    private void OnTouchUp(object? sender, TouchEventArgs e)
    {
        int id = TouchBase | (int)e.TouchDevice.Id;
        if (_active.Contains(id))
        {
            var p = e.GetTouchPoint(this).Position;
            _engine.PointerMove(id, p.X, p.Y);
            EndPointer(id, cancel: false);
            Log.Info($"board touch up id={id}");
        }

        ReleaseTouchCapture(e.TouchDevice);
        e.Handled = true;
    }

    private void OnLostTouchCapture(object? sender, TouchEventArgs e)
    {
        int id = TouchBase | (int)e.TouchDevice.Id;
        if (_active.Contains(id))
        {
            EndPointer(id, cancel: true);
            Log.Info($"board touch capture lost id={id}");
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        // 笔/手指的鼠标提升事件、以及正在书写的触摸，都要让位
        if (e.StylusDevice is not null || _active.Count > 0 || _mouseDown) return;

        if (TryDismissPopup())
        {
            e.Handled = true;
            return;
        }

        var p = e.GetPosition(this);
        if (p.Y < 0 || p.Y >= DrawingHeight) return;

        if (!_engine.PointerDown(MousePointerId, p.X, p.Y)) return;

        _mouseDown = true;
        _active.Add(MousePointerId);
        CaptureMouse();
        Log.Info($"board mouse down at {p.X:0.0},{p.Y:0.0}");
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_mouseDown || e.StylusDevice is not null) return;
        var p = e.GetPosition(this);
        _engine.PointerMove(MousePointerId, p.X, p.Y);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_mouseDown) return;

        var p = e.GetPosition(this);
        _engine.PointerMove(MousePointerId, p.X, p.Y);
        _mouseDown = false;
        _active.Remove(MousePointerId);
        ReleaseMouseCapture();
        _engine.PointerUp(MousePointerId);
        Log.Info($"board mouse up at {p.X:0.0},{p.Y:0.0}");
        e.Handled = true;
    }

    private void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (!_mouseDown) return;
        _mouseDown = false;
        _active.Remove(MousePointerId);
        _engine.PointerCancel(MousePointerId);
        Log.Info("board mouse capture lost");
    }

    private void EndPointer(int id, bool cancel)
    {
        if (!_active.Remove(id)) return;
        if (cancel) _engine.PointerCancel(id);
        else _engine.PointerUp(id);
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        Log.Info("undo clicked");
        _engine.Undo();
    }

    private void OnEraserClick(object sender, RoutedEventArgs e)
    {
        if (_eraserArmed)
        {
            ClearRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        Log.Info("eraser clicked -> armed");
        _eraserArmed = true;
        _engine.SetTool(InkTool.Eraser);
    }

    private void OnCollapseClick(object sender, RoutedEventArgs e)
    {
        Log.Info("collapse clicked");
        CollapseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPrevPageClick(object sender, RoutedEventArgs e)
    {
        var doc = _engine.Document;
        if (doc.CurrentIndex > 0) doc.GoTo(doc.CurrentIndex - 1);
    }

    private void OnNextPageClick(object sender, RoutedEventArgs e)
    {
        var doc = _engine.Document;
        if (doc.CurrentIndex < doc.PageCount - 1) doc.GoTo(doc.CurrentIndex + 1);
        else doc.AddPageAfterCurrent();
    }

    private void OnPageBoxClick(object sender, RoutedEventArgs e) => PageMenuRequested?.Invoke(this, EventArgs.Empty);

    private void OnPageChanged()
    {
        UpdatePageText();
        Dispatcher.BeginInvoke(new Action(() => Ink.InvalidateVisual()));
    }

    private void UpdatePageText()
    {
        var doc = _engine.Document;
        PageText.Text = $"{doc.CurrentIndex + 1}/{doc.PageCount}";
    }
}