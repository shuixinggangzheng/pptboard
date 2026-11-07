using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PPTBoard.Core.Models;

namespace PPTBoard.App.Board;

/// <summary>色相环：外圈彩虹、中心当前色预览、白色标记当前色相。支持鼠标与触摸拖动。</summary>
public sealed class HueWheel : FrameworkElement
{
    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue), typeof(double), typeof(HueWheel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PreviewColorProperty = DependencyProperty.Register(
        nameof(PreviewColor), typeof(Color), typeof(HueWheel),
        new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsInteractiveProperty = DependencyProperty.Register(
        nameof(IsInteractive), typeof(bool), typeof(HueWheel), new PropertyMetadata(true));

    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public Color PreviewColor
    {
        get => (Color)GetValue(PreviewColorProperty);
        set => SetValue(PreviewColorProperty, value);
    }

    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public event Action<double>? HueChanged;

    public HueWheel()
    {
        if (!IsInteractive) return;
        MouseLeftButtonDown += (_, e) => { CaptureMouse(); Pick(e.GetPosition(this)); e.Handled = true; };
        MouseMove += (_, e) => { if (IsMouseCaptured) Pick(e.GetPosition(this)); };
        MouseLeftButtonUp += (_, _) => ReleaseMouseCapture();
        TouchDown += (_, e) => { CaptureTouch(e.TouchDevice); Pick(e.GetTouchPoint(this).Position); e.Handled = true; };
        TouchMove += (_, e) => { if (e.TouchDevice.Captured == e.TouchDevice) Pick(e.GetTouchPoint(this).Position); e.Handled = true; };
        TouchUp += (_, e) => { e.TouchDevice.Capture(null); e.Handled = true; };
    }

    private void Pick(Point p)
    {
        if (!IsInteractive) return;
        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - 2;
        double dx = p.X - ActualWidth / 2;
        double dy = p.Y - ActualHeight / 2;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < radius * 0.55 || distance > radius + 8) return;

        double angle = Math.Atan2(dy, dx) * 180 / Math.PI + 90;
        if (angle < 0) angle += 360;
        Hue = angle;
        HueChanged?.Invoke(angle);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double outer = size / 2 - 2;
        double thickness = Math.Max(10, size * 0.10);
        double mid = outer - thickness / 2;
        double inner = outer - thickness;

        for (int deg = 0; deg < 360; deg += 3)
        {
            var c = ColorMath.HsvToRgb(deg, 1, 1);
            var brush = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            var pen = new Pen(brush, thickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
            dc.DrawLine(pen, PointOn(center, mid, deg), PointOn(center, mid, deg + 3.2));
        }

        double discRadius = Math.Max(1, inner - 4);
        dc.DrawEllipse(new SolidColorBrush(PreviewColor), null, center, discRadius, discRadius);

        var marker = PointOn(center, mid, Hue);
        dc.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 1), marker, 6, 6);
    }

    private static Point PointOn(Point center, double radius, double degrees)
    {
        double rad = (degrees - 90) * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
    }
}
