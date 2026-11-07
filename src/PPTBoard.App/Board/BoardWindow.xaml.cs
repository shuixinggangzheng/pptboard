using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PPTBoard.App.Host;
using PPTBoard.App.Interop;
using PPTBoard.Core.Geometry;
using PPTBoard.Ink;

namespace PPTBoard.App.Board;

public partial class BoardWindow : OverlayWindow
{
    private const int WM_POINTERUPDATE = 0x0245;
    private const int WM_POINTERDOWN = 0x0246;
    private const int WM_POINTERUP = 0x0247;
    private const int WM_POINTERCAPTURECHANGED = 0x024C;
    private const int MousePointerId = 0x7FFF;

    private readonly InkEngine _engine;
    private HwndSource? _source;
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
            Log.Info($"board preview down at {e.GetPosition(this)} source={e.OriginalSource?.GetType().Name}");

        Ink.Attach(engine);
        engine.Document.CurrentPageChanged += OnPageChanged;
        engine.Document.PagesChanged += OnPageChanged;
        UpdatePageText();
    }

    public double DrawingHeight => Height - 56;

    public void SetBoardActive(bool active) => EraserButton.Background = active ? Brushes.Transparent : Brushes.Transparent;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _source = PresentationSource.FromVisual(this) as HwndSource;
        _source?.AddHook(WndProc);

        var hwnd = _source?.Handle ?? IntPtr.Zero;
        Interop.NativeMethods.GetWindowRect(hwnd, out var rect);
        Log.Info($"board hwnd={hwnd} rect=({rect.Left},{rect.Top})-({rect.Right},{rect.Bottom}) size={Width}x{Height}");
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.StylusDevice is not null) return;
        if (IsPopupOpen?.Invoke() == true)
        {
            DismissPopups?.Invoke();
            e.Handled = true;
            return;
        }

        var p = e.GetPosition(this);
        if (p.Y >= DrawingHeight) return;
        _mouseDown = true;
        CaptureMouse();
        _engine.PointerDown(MousePointerId, p.X, p.Y);
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
        _mouseDown = false;
        ReleaseMouseCapture();
        _engine.PointerUp(MousePointerId);
        e.Handled = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg is not (WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP or WM_POINTERCAPTURECHANGED))
            return IntPtr.Zero;

        int pointerId = (int)(wParam.ToInt64() & 0xFFFF);
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        int px = (short)(lParam.ToInt64() & 0xFFFF);
        int py = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        double x = px / scale;
        double y = py / scale;

        if (msg == WM_POINTERCAPTURECHANGED)
        {
            _engine.PointerCancel(pointerId);
            return IntPtr.Zero;
        }

        if (msg == WM_POINTERDOWN)
        {
            if (IsPopupOpen?.Invoke() == true)
            {
                DismissPopups?.Invoke();
                handled = true;
                return IntPtr.Zero;
            }

            if (y >= DrawingHeight) return IntPtr.Zero;
            _engine.PointerDown(pointerId, x, y);
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_POINTERUPDATE)
        {
            if (y >= DrawingHeight) return IntPtr.Zero;
            _engine.PointerMove(pointerId, x, y);
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_POINTERUP)
        {
            _engine.PointerUp(pointerId);
            handled = true;
        }

        return IntPtr.Zero;
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
