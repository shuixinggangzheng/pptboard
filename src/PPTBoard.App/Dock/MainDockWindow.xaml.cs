using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;
using PPTBoard.Core.Models;

namespace PPTBoard.App.Dock;

public partial class MainDockWindow : OverlayWindow
{
    public event EventHandler? ColorRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? NextRequested;
    public event EventHandler? PenRequested;
    public event EventHandler? BoardRequested;
    public event EventHandler? EraserRequested;
    public event EventHandler? EndShowRequested;
    public event EventHandler? PenMenuRequested;

    private bool _penLongPress;

    public MainDockWindow(LayoutSpec layout)
    {
        InitializeComponent();
        Left = layout.MainDock.X;
        Top = layout.MainDock.Y;
        MouseLeftButtonDown += (_, e) => Log.Info($"dock mouse down at {e.GetPosition(this)}");

        var hold = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        hold.Tick += (_, _) =>
        {
            hold.Stop();
            _penLongPress = true;
            PenMenuRequested?.Invoke(this, EventArgs.Empty);
        };
        PenButton.PreviewMouseLeftButtonDown += (_, _) =>
        {
            _penLongPress = false;
            hold.Start();
        };
        PenButton.PreviewMouseLeftButtonUp += (_, _) => hold.Stop();
        PenButton.MouseLeave += (_, _) => hold.Stop();
    }

    public void SetColor(InkColor color)
    {
        ColorDot.Fill = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
    }

    public void SetNavigable(bool canNavigate)
    {
        PrevButton.IsEnabled = canNavigate;
        NextButton.IsEnabled = canNavigate;
    }

    public void SetPenSupported(bool supported)
    {
        PenButton.IsEnabled = supported;
    }

    public void SetEndEnabled(bool enabled)
    {
        EndButton.IsEnabled = enabled;
    }

    public void SetEraserActive(bool active)
    {
        EraserButton.Background = active
            ? new SolidColorBrush(Color.FromArgb(0x33, 0x2F, 0x7C, 0xF6))
            : Brushes.Transparent;
    }

    public void SetPenActive(bool active)
    {
        PenButton.Background = active
            ? new SolidColorBrush(Color.FromArgb(0x33, 0x2F, 0x7C, 0xF6))
            : Brushes.Transparent;
    }

    public void SetBoardActive(bool active)
    {
        BoardButton.Background = active
            ? new SolidColorBrush(Color.FromArgb(0x33, 0x2F, 0x7C, 0xF6))
            : Brushes.Transparent;
    }

    private void OnColorClick(object sender, RoutedEventArgs e) => ColorRequested?.Invoke(this, EventArgs.Empty);
    private void OnPrevClick(object sender, RoutedEventArgs e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
    private void OnNextClick(object sender, RoutedEventArgs e) => NextRequested?.Invoke(this, EventArgs.Empty);
    private void OnPenClick(object sender, RoutedEventArgs e)
    {
        // 长按已经弹过菜单时，抬起产生的 Click 不再切换笔
        if (_penLongPress)
        {
            _penLongPress = false;
            return;
        }
        PenRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnEraserClick(object sender, RoutedEventArgs e) => EraserRequested?.Invoke(this, EventArgs.Empty);

    private void OnBoardClick(object sender, RoutedEventArgs e)
    {
        Log.Info("dock board clicked");
        BoardRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnEndClick(object sender, RoutedEventArgs e) => EndShowRequested?.Invoke(this, EventArgs.Empty);
}
