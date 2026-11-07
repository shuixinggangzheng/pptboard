using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PPTBoard.App.Board;
using PPTBoard.App.Dock;
using PPTBoard.Core.Models;
using PPTBoard.Core.Contracts;
using PPTBoard.Core.Geometry;
using PPTBoard.Ink;

namespace PPTBoard.App.Host;

/// <summary>浮层生命周期：检测放映、显示/隐藏 dock、画板开关（后续接入）。</summary>
public sealed class SlideshowHost : IDisposable
{
    private readonly LayoutSpec _layout;
    private readonly InkEngine _ink;
    private readonly ISlideshowBridge _bridge;
    private readonly WindowStack _stack = new();
    private readonly DispatcherTimer _showPoll = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly DispatcherTimer _processPoll = new() { Interval = TimeSpan.FromMilliseconds(1000) };

    private MainDockWindow? _dock;
    private BoardWindow? _board;
    private MarginCatcher? _leftMargin;
    private MarginCatcher? _rightMargin;
    private PenDockWindow? _penDock;
    private ColorPanelWindow? _colorPanel;
    private ClearSliderWindow? _clearSlider;
    private PageMenuWindow? _pageMenu;
    private PenOptionsWindow? _penOptions;
    private PopoverCatcher? _popoverCatcher;
    private DispatcherTimer? _boardAnim;
    private bool _boardOpen;
    private bool _nativePenOn;
    private bool _nativeEraserOn;
    private bool _lastBridgeAvailable;
    private string? _lastBridgeError;
    private string? _lastBridgeDiagnostics;
    private bool _windowFallbackLogged;
    private int _wpsMissingTicks;
    private bool _disposed;

    public SlideshowHost(LayoutSpec layout, InkEngine ink, ISlideshowBridge bridge)
    {
        _layout = layout;
        _ink = ink;
        _bridge = bridge;
    }

    public Task<bool> InitializeAsync() => Task.FromResult(true);

    public void Start()
    {
        Log.Info("host start");
        _dock = new MainDockWindow(_layout);
        Log.Info("dock created");
        _dock.ColorRequested += (_, _) =>
        {
            if (_colorPanel?.IsVisible == true)
            {
                _colorPanel.Hide();
                return;
            }
            OpenColorPanel(_layout.MainDock);
        };
        _dock.PreviousRequested += (_, _) => { if (_bridge.CanNavigate) _bridge.Previous(); };
        _dock.NextRequested += (_, _) => { if (_bridge.CanNavigate) _bridge.Next(); };
        _dock.PenRequested += (_, _) => ToggleNativePen();
        _dock.PenMenuRequested += (_, _) => OpenPenMenu();
        _dock.BoardRequested += (_, _) => ToggleBoard();
        _dock.EraserRequested += (_, _) => ToggleEraser();
        _dock.EndShowRequested += (_, _) => { if (_bridge.CanNavigate) _bridge.ExitShow(); };

        _dock.SetNavigable(_bridge.CanNavigate);
        _dock.SetPenSupported(_bridge.NativePenSupported);
        _dock.SetColor(_ink.Style.Color);
        _dock.SetBoardActive(false);

        _stack.Add(_dock);

        _colorPanel = new ColorPanelWindow(_layout);
        _colorPanel.ColorChanged += OnCustomColorChanged;
        _colorPanel.IsVisibleChanged += (_, _) =>
        {
            if (!_colorPanel.IsVisible)
            {
                _popoverCatcher?.Hide();
                Log.Info("color panel hidden");
            }
        };

        _clearSlider = new ClearSliderWindow(_layout);
        _clearSlider.Cleared += (_, _) => _ink.ClearCurrentPage();

        _showPoll.Tick += (_, _) => PollShow();
        _processPoll.Tick += (_, _) => PollProcess();
        _showPoll.Start();
        _processPoll.Start();

        if (Log.DebugShow)
        {
            Log.Info("debug show dock");
            ShowDock();
            Log.Info($"dock visible={_dock.IsVisible} left={_dock.Left} top={_dock.Top} opacity={_dock.Opacity}");
        }

        if (Environment.GetEnvironmentVariable("PPTBOARD_DEBUG_BOARD") == "1")
        {
            Log.Info("debug open board");
            OpenBoard();
        }

        if (Environment.GetEnvironmentVariable("PPTBOARD_DEBUG_COLOR") == "1")
        {
            Log.Info("debug open color panel");
            OpenColorPanel(_layout.MainDock);
            LogColorZOrder();
        }

        if (Environment.GetEnvironmentVariable("PPTBOARD_DEBUG_TOGGLE") == "1")
        {
            var hide = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            hide.Tick += (_, _) =>
            {
                hide.Stop();
                Log.Info("debug hide dock");
                HideDock();
            };
            hide.Start();

            var show = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            show.Tick += (_, _) =>
            {
                show.Stop();
                Log.Info("debug show dock again");
                ShowDock();
                var probe = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                probe.Tick += (_, _) =>
                {
                    probe.Stop();
                    Log.Info($"debug dock re-shown visible={_dock?.IsVisible} opacity={_dock?.Opacity}");
                };
                probe.Start();
            };
            show.Start();
        }

        Log.Info("host start done");
    }

    private void PollShow()
    {
        if (_disposed) return;
        bool comRunning = _bridge.IsShowRunning;
        bool windowRunning = PPTBoard.Wps.ShowWindowProbe.IsRunning();
        bool running = comRunning || windowRunning;
        if (windowRunning && !comRunning && !_windowFallbackLogged)
        {
            _windowFallbackLogged = true;
            Log.Info("slideshow detected by window fallback (COM unavailable)");
        }
        if (!running) _windowFallbackLogged = false;

        if (_bridge.IsAvailable != _lastBridgeAvailable)
        {
            _lastBridgeAvailable = _bridge.IsAvailable;
            Log.Info($"bridge available={_lastBridgeAvailable}");
        }

        if (_bridge is PPTBoard.Wps.WpsBridge wpsBridge)
        {
            var error = wpsBridge.LastError;
            if (error is not null && error != _lastBridgeError)
            {
                _lastBridgeError = error;
                Log.Info("bridge: " + error);
            }

            if (!running && wpsBridge.Diagnostics.Length > 0 && wpsBridge.Diagnostics != _lastBridgeDiagnostics)
            {
                _lastBridgeDiagnostics = wpsBridge.Diagnostics;
                Log.Info("bridge diag: " + wpsBridge.Diagnostics);
            }
        }

        bool shouldShow = Log.DebugShow || running;

        if (_dock is not null)
        {
            bool pptEnabled = !_boardOpen;
            _dock.SetNavigable(pptEnabled && _bridge.CanNavigate);
            // 画板打开时“笔”仍可用，但只控制白板画笔，不碰放映指针
            _dock.SetPenSupported(pptEnabled ? _bridge.NativePenSupported : true);
            _dock.SetEndEnabled(pptEnabled && _bridge.CanNavigate);
        }

        if (!running && (_nativePenOn || _nativeEraserOn))
        {
            _nativePenOn = false;
            _nativeEraserOn = false;
            _dock?.SetPenActive(false);
            _dock?.SetEraserActive(false);
        }

        if (shouldShow)
        {
            ShowDock();
        }
        else
        {
            HideDock();
        }
    }

    private void PollProcess()
    {
        if (_disposed || Log.DebugShow) return;

        bool wpsRunning = Process.GetProcessesByName("wpp").Length > 0;
        bool powerpointRunning = Process.GetProcessesByName("POWERPNT").Length > 0;
        if (wpsRunning || powerpointRunning)
        {
            _wpsMissingTicks = 0;
            return;
        }

        if (++_wpsMissingTicks >= 2)
        {
            Log.Info("WPS/PowerPoint gone, shutdown");
            Application.Current.Shutdown();
        }
    }

    private void ShowDock()
    {
        if (_dock is null || _dock.IsVisible) return;

        Log.Info("ShowDock");
        _dock.BeginAnimation(Window.OpacityProperty, null);
        _dock.Left = _layout.MainDock.X;
        _dock.Top = _layout.MainDock.Y;
        _dock.Opacity = 0;
        _dock.Show();
        _dock.BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        _stack.BringToFront();

        var hwnd = new System.Windows.Interop.WindowInteropHelper(_dock).Handle;
        bool rectOk = Interop.NativeMethods.GetWindowRect(hwnd, out var rect);
        Log.Info($"dock shown hwnd={hwnd} rectOk={rectOk} rect=({rect.Left},{rect.Top})-({rect.Right},{rect.Bottom}) topmost={_dock.Topmost} opacity={_dock.Opacity}");

        var probe = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        probe.Tick += (_, _) =>
        {
            probe.Stop();
            bool ok = Interop.NativeMethods.GetWindowRect(hwnd, out var r2);
            Log.Info($"dock after800 rectOk={ok} rect=({r2.Left},{r2.Top})-({r2.Right},{r2.Bottom}) vis={Interop.NativeMethods.IsWindowVisible(hwnd)} opacity={_dock.Opacity}");
        };
        probe.Start();
    }

    private void HideDock()
    {
        CloseBoard(immediate: true);
        if (_dock is null || !_dock.IsVisible) return;
        Log.Info("HideDock");
        var animation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(140));
        animation.Completed += (_, _) =>
        {
            _dock.Hide();
            _dock.BeginAnimation(Window.OpacityProperty, null);
            _dock.Opacity = 0;
        };
        _dock.BeginAnimation(Window.OpacityProperty, animation);
    }

    private void AnimateBoard(double from, double to, int milliseconds, bool easeOut, Action completed)
    {
        if (_board is null) return;
        _boardAnim?.Stop();
        var sw = Stopwatch.StartNew();
        _board.Top = from;
        var timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(15) };
        _boardAnim = timer;
        timer.Tick += (_, _) =>
        {
            double t = Math.Clamp(sw.Elapsed.TotalMilliseconds / milliseconds, 0, 1);
            double eased = easeOut ? 1 - Math.Pow(1 - t, 3) : t * t * t;
            _board.Top = from + (to - from) * eased;
            if (t >= 1)
            {
                timer.Stop();
                _boardAnim = null;
                _board.Top = to;
                completed();
            }
        };
        timer.Start();
    }

    private void ToggleEraser()
    {
        // 画板开着时控制白板橡皮；画板关着时控制放映原生橡皮，绝不自动打开画板
        if (_boardOpen)
        {
            bool erasing = _ink.Tool == InkTool.Eraser;
            _ink.SetTool(erasing ? InkTool.Pen : InkTool.Eraser);
            _dock?.SetEraserActive(!erasing);
            _dock?.SetPenActive(erasing);
            Log.Info("dock eraser -> whiteboard " + (erasing ? "pen" : "eraser"));
            return;
        }

        if (!_bridge.CanNavigate) return;

        if (_nativeEraserOn)
        {
            _bridge.SetNativePointer(1);
            _nativeEraserOn = false;
            _dock?.SetEraserActive(false);
            Log.Info("dock eraser -> arrow");
            return;
        }

        // WPS 没有橡皮的 PointerType，使用放映快捷键 Ctrl+E（PowerPoint 同理）
        Interop.KeyboardInput.SendEraser();
        _nativeEraserOn = true;
        _nativePenOn = false;
        _dock?.SetEraserActive(true);
        _dock?.SetPenActive(false);
        Log.Info("dock eraser -> Ctrl+E");
    }

    private void ToggleBoard()
    {
        if (_boardOpen) CloseBoard(immediate: false);
        else OpenBoard();
    }

    private bool DismissPopupsIfOpen()
    {
        if (!IsPopupVisible()) return false;
        HidePopups();
        return true;
    }

    private bool IsPopupVisible() =>
        (_pageMenu?.IsVisible ?? false) || (_colorPanel?.IsVisible ?? false) || (_clearSlider?.IsVisible ?? false) || (_penOptions?.IsVisible ?? false);

    private void HidePopups()
    {
        _pageMenu?.Hide();
        _colorPanel?.Hide();
        _clearSlider?.Hide();
        _penOptions?.Hide();
    }

    private void TogglePageMenu()
    {
        if (_pageMenu is null)
        {
            _pageMenu = new PageMenuWindow(_layout, _ink);
            _stack.Add(_pageMenu);
        }

        if (_pageMenu.IsVisible)
        {
            _pageMenu.Hide();
            return;
        }

        _stack.MoveToTop(_pageMenu);
        _pageMenu.Open();
        _stack.BringToFront();
        Log.Info("page menu opened");
    }

    private void OpenPenMenu()
    {
        if (_boardOpen) return;
        if (!_bridge.CanNavigate) return;
        if (_penOptions is null)
        {
            _penOptions = new PenOptionsWindow(_layout);
            _penOptions.OptionChosen += option =>
            {
                _penOptions.Hide();
                ApplyPenOption(option);
            };
            _stack.Add(_penOptions);
        }

        _penOptions.Open();
        _stack.MoveToTop(_penOptions);
        _stack.BringToFront();
    }

    private void LogColorZOrder()
    {
        if (_colorPanel is null || _popoverCatcher is null) return;
        var panelHwnd = new System.Windows.Interop.WindowInteropHelper(_colorPanel).Handle;
        var catcherHwnd = new System.Windows.Interop.WindowInteropHelper(_popoverCatcher).Handle;
        Interop.NativeMethods.GetWindowRect(panelHwnd, out var pr);
        var inside = Interop.NativeMethods.WindowFromPoint(new Interop.NativeMethods.POINT
        {
            X = pr.Left + (pr.Right - pr.Left) / 2,
            Y = pr.Top + (pr.Bottom - pr.Top) / 2
        });
        var outside = Interop.NativeMethods.WindowFromPoint(new Interop.NativeMethods.POINT { X = 40, Y = 40 });
        Log.Info($"color z: panel={panelHwnd} catcher={catcherHwnd} hitPanel={inside} hitOutside={outside}");
    }

    private void ApplyPenOption(string option)
    {
        switch (option)
        {
            case "pen":
                _bridge.SetNativePointer(2);
                _bridge.SetNativePenColor(_ink.Style.Color);
                _nativePenOn = true;
                _dock?.SetPenActive(true);
                break;
            case "eraser":
                _bridge.SetNativePointer(3);
                _nativePenOn = false;
                _dock?.SetPenActive(false);
                break;
            case "arrow":
                _bridge.SetNativePointer(1);
                _nativePenOn = false;
                _dock?.SetPenActive(false);
                break;
            case "color":
                OpenColorPanel(_layout.MainDock);
                break;
        }
    }

    private void ToggleNativePen()
    {
        if (_boardOpen)
        {
            _ink.SetTool(InkTool.Pen);
            _dock?.SetPenActive(true);
            _dock?.SetEraserActive(false);
            Log.Info("dock pen -> whiteboard pen");
            return;
        }

        if (!_bridge.NativePenSupported) return;
        _nativePenOn = !_nativePenOn;
        _bridge.SetNativePen(_nativePenOn);
        if (_nativePenOn)
        {
            _bridge.SetNativePenColor(_ink.Style.Color);
            _nativeEraserOn = false;
            _dock?.SetEraserActive(false);
        }
        _dock?.SetPenActive(_nativePenOn);
    }

    private void OpenColorPanel(RectD anchor)
    {
        if (_colorPanel is null) return;

        if (_popoverCatcher is null)
        {
            _popoverCatcher = new PopoverCatcher(_layout);
            _popoverCatcher.Pressed += (_, _) =>
            {
                Log.Info("popover catcher pressed -> close color panel");
                _colorPanel.Hide();
            };
            _stack.Add(_popoverCatcher);
        }

        _popoverCatcher.Show();
        _stack.Add(_colorPanel);
        _stack.MoveToTop(_popoverCatcher);
        _stack.MoveToTop(_colorPanel);
        _colorPanel.Open(_ink.Style.Color, anchor);
        Log.Info($"color panel open at {anchor} visible={_colorPanel.IsVisible} left={_colorPanel.Left} top={_colorPanel.Top}");
        _stack.BringToFront();
        LogColorZOrder();
    }

    private void OnCustomColorChanged(InkColor color)
    {
        _ink.SetStyle(new PenStyle(color, _ink.Style.Width));
        _penDock?.SelectColor(color);
        _dock?.SetColor(color);
        if (_nativePenOn) _bridge.SetNativePenColor(color);
    }

    private void OpenBoard()
    {
        if (_boardOpen || _dock is null) return;
        _boardOpen = true;

        var boardRect = _layout.Board;
        var leftRect = new RectD(0, 0, boardRect.X, _layout.ScreenHeight);
        var rightRect = new RectD(boardRect.Right, 0, _layout.ScreenWidth - boardRect.Right, _layout.ScreenHeight);

        if (_leftMargin is null)
        {
            _leftMargin = new MarginCatcher(leftRect);
            _leftMargin.Pressed += (_, _) =>
            {
                if (DismissPopupsIfOpen()) return;
                Log.Info("left margin pressed -> close board");
                CloseBoard(immediate: false);
            };
            _stack.AddFirst(_leftMargin);

            _rightMargin = new MarginCatcher(rightRect);
            _rightMargin.Pressed += (_, _) =>
            {
                if (DismissPopupsIfOpen()) return;
                Log.Info("right margin pressed -> close board");
                CloseBoard(immediate: false);
            };
            _stack.AddFirst(_rightMargin);
        }

        _leftMargin.Show();
        _rightMargin!.Show();

        if (_board is null)
        {
            _board = new BoardWindow(_layout, _ink);
            _board.CollapseRequested += (_, _) => CloseBoard(immediate: false);
            _board.ClearRequested += (_, _) =>
            {
                _clearSlider?.Open();
                if (_clearSlider is not null) _stack.MoveToTop(_clearSlider);
                _stack.BringToFront();
            };
            _board.PageMenuRequested += (_, _) => TogglePageMenu();
            _stack.Add(_board);
        }

        if (_penDock is null)
        {
            _penDock = new PenDockWindow(_layout, _ink);
            _penDock.StyleChanged += (_, _) =>
            {
                _dock?.SetColor(_ink.Style.Color);
                _dock?.SetEraserActive(false);
                if (_boardOpen) _dock?.SetPenActive(true);
                if (_nativePenOn) _bridge.SetNativePenColor(_ink.Style.Color);
            };
            _penDock.CustomColorRequested += (_, _) =>
            {
                Log.Info("custom color requested");
                OpenColorPanel(_layout.PenDock);
            };
            _stack.Add(_penDock);
            _stack.Add(_clearSlider!);
            _stack.Add(_colorPanel!);
        }

        _board.BeginAnimation(Window.TopProperty, null);
        _board.Top = -_layout.Board.Height;
        _board.Show();
        AnimateBoard(-_layout.Board.Height, 0, 260, easeOut: true, () =>
        {
            Log.Info("board slide done");
            _stack.BringToFront();
        });

        _board.IsPopupOpen = () => (_pageMenu?.IsVisible ?? false) || (_colorPanel?.IsVisible ?? false) || (_clearSlider?.IsVisible ?? false);
        _board.DismissPopups = HidePopups;

        _penDock!.Refresh();
        _penDock.Show();
        _penDock.RaiseTopmost();

        // 画板打开时先把放映原生笔/橡皮复位，避免两层同时响应
        if (_nativePenOn || _nativeEraserOn)
        {
            _bridge.SetNativePointer(1);
            _nativePenOn = false;
            _nativeEraserOn = false;
        }

        _ink.SetTool(InkTool.Pen);
        _dock.SetBoardActive(true);
        _dock.SetPenActive(true);
        _dock.SetEraserActive(false);
        _dock.SetNavigable(false);
        _dock.SetPenSupported(true);
        _dock.SetEndEnabled(false);
        _stack.BringToFront();
        Log.Info($"board opened rect={_layout.Board}");

        var probe = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        probe.Tick += (_, _) =>
        {
            probe.Stop();
            if (_board is null) return;
            var boardHwnd = new System.Windows.Interop.WindowInteropHelper(_board).Handle;
            Interop.NativeMethods.GetWindowRect(boardHwnd, out var r);
            var pt = new Interop.NativeMethods.POINT { X = r.Left + 44, Y = r.Bottom - 25 };
            var hit = Interop.NativeMethods.WindowFromPoint(pt);
            Log.Info($"board after500 top={_board.Top} rect=({r.Left},{r.Top})-({r.Right},{r.Bottom}) boardHwnd={boardHwnd} hitAtGrayBar={hit}");
        };
        probe.Start();
    }

    private void CloseBoard(bool immediate)
    {
        if (!_boardOpen) return;
        _boardOpen = false;
        _ink.CancelActivePointers();
        _ink.SetTool(InkTool.Pen);
        if (_nativeEraserOn)
        {
            _bridge.SetNativePointer(1);
            _nativeEraserOn = false;
        }
        _dock?.SetBoardActive(false);
        _dock?.SetEraserActive(false);
        _dock?.SetPenActive(false);
        _dock?.SetNavigable(_bridge.CanNavigate);
        _dock?.SetPenSupported(_bridge.NativePenSupported);
        _dock?.SetEndEnabled(_bridge.CanNavigate);
        _penDock?.Hide();
        _clearSlider?.Hide();
        _colorPanel?.Hide();
        _pageMenu?.Hide();
        _penOptions?.Hide();

        if (_board is null) return;

        if (immediate)
        {
            _board.BeginAnimation(Window.TopProperty, null);
            _board.Hide();
            _board.Top = -_layout.Board.Height;
            _leftMargin?.Hide();
            _rightMargin?.Hide();
            return;
        }

        AnimateBoard(_board.Top, -_layout.Board.Height, 220, easeOut: false, () =>
        {
            _board.Top = -_layout.Board.Height;
            _board.Hide();
            _leftMargin?.Hide();
            _rightMargin?.Hide();
        });
    }

    public void Dispose()
    {
        _disposed = true;
        _showPoll.Stop();
        _processPoll.Stop();
        _board?.Close();
        _pageMenu?.Close();
        _penOptions?.Close();
        _popoverCatcher?.Close();
        _leftMargin?.Close();
        _rightMargin?.Close();
        _dock?.Close();
    }
}
