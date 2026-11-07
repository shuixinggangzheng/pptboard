using System.Windows;
using System.Windows.Input;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;

namespace PPTBoard.App.Board;

public partial class ClearSliderWindow : OverlayWindow
{
    private bool _dragging;
    private double _thumbX = 4;

    public event EventHandler? Cleared;

    public ClearSliderWindow(LayoutSpec layout)
    {
        InitializeComponent();
        Left = layout.ClearSlider.X;
        Top = layout.ClearSlider.Y;
    }

    public void Open()
    {
        _thumbX = 4;
        Thumb.Margin = new Thickness(4, 0, 0, 0);
        if (!IsVisible) Show();
        RaiseTopmost();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var p = e.GetPosition(this);
        if (p.X <= _thumbX + 44)
        {
            _dragging = true;
            CaptureMouse();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;
        var p = e.GetPosition(this);
        _thumbX = Math.Clamp(p.X - 18, 4, Width - 40);
        Thumb.Margin = new Thickness(_thumbX, 0, 0, 0);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();

        if (_thumbX >= Width - 64)
        {
            Hide();
            Cleared?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _thumbX = 4;
            Thumb.Margin = new Thickness(4, 0, 0, 0);
        }
    }
}
