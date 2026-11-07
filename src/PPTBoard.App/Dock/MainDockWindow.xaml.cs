using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    public event EventHandler? EndShowRequested;

    public MainDockWindow(LayoutSpec layout)
    {
        InitializeComponent();
        Left = layout.MainDock.X;
        Top = layout.MainDock.Y;
        MouseLeftButtonDown += (_, e) => Log.Info($"dock mouse down at {e.GetPosition(this)}");
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

    public void SetBoardActive(bool active)
    {
        BoardButton.Background = active
            ? new SolidColorBrush(Color.FromArgb(0x33, 0x2F, 0x7C, 0xF6))
            : Brushes.Transparent;
    }

    private void OnColorClick(object sender, RoutedEventArgs e) => ColorRequested?.Invoke(this, EventArgs.Empty);
    private void OnPrevClick(object sender, RoutedEventArgs e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
    private void OnNextClick(object sender, RoutedEventArgs e) => NextRequested?.Invoke(this, EventArgs.Empty);
    private void OnPenClick(object sender, RoutedEventArgs e) => PenRequested?.Invoke(this, EventArgs.Empty);
    private void OnBoardClick(object sender, RoutedEventArgs e)
    {
        Log.Info("dock board clicked");
        BoardRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnEndClick(object sender, RoutedEventArgs e) => EndShowRequested?.Invoke(this, EventArgs.Empty);
}
