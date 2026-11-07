using System.Windows;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;

namespace PPTBoard.App.Dock;

public partial class PenOptionsWindow : OverlayWindow
{
    public event Action<string>? OptionChosen;

    public PenOptionsWindow(LayoutSpec layout)
    {
        InitializeComponent();
        double penButtonCenter = layout.MainDock.X + 13 + 3 * 44 + 20;
        Left = Math.Clamp(penButtonCenter - Width / 2, 12, layout.ScreenWidth - Width - 12);
        Top = layout.MainDock.Y - Height - 12;
    }

    public void Open()
    {
        if (!IsVisible) Show();
        RaiseTopmost();
    }

    private void OnPenClick(object sender, RoutedEventArgs e) => OptionChosen?.Invoke("pen");
    private void OnEraserClick(object sender, RoutedEventArgs e) => OptionChosen?.Invoke("eraser");
    private void OnArrowClick(object sender, RoutedEventArgs e) => OptionChosen?.Invoke("arrow");
    private void OnColorClick(object sender, RoutedEventArgs e) => OptionChosen?.Invoke("color");
}
