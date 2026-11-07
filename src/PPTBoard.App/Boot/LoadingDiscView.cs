using System.Windows;
using System.Windows.Media;

namespace PPTBoard.App.Boot;

/// <summary>右下角 loading 圆盘：蓝色进度环 + 绿色渐变勾 / 红色叉。</summary>
public sealed class LoadingDiscView : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(LoadingDiscView),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CheckProgressProperty = DependencyProperty.Register(
        nameof(CheckProgress), typeof(double), typeof(LoadingDiscView),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FailedProperty = DependencyProperty.Register(
        nameof(Failed), typeof(bool), typeof(LoadingDiscView),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double CheckProgress
    {
        get => (double)GetValue(CheckProgressProperty);
        set => SetValue(CheckProgressProperty, value);
    }

    public bool Failed
    {
        get => (bool)GetValue(FailedProperty);
        set => SetValue(FailedProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;

        var center = new Point(size / 2, size / 2);
        double discRadius = size / 2 - 2;
        double ringRadius = discRadius - 4;

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(232, 18, 20, 26)), null, center, discRadius, discRadius);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)), 3), center, ringRadius, ringRadius);

        if (Progress > 0.001)
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xF6));
            var pen = new Pen(brush, 3)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawGeometry(null, pen, BuildArc(center, ringRadius, -90, 359.99 * Math.Clamp(Progress, 0, 1)));
        }

        if (Failed)
        {
            var red = new Pen(new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D)), 3.6)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            DrawPartial(dc, red, new[] { new Point(26, 26), new Point(46, 46) }, CheckProgress + 0.0);
            DrawPartial(dc, red, new[] { new Point(46, 26), new Point(26, 46) }, CheckProgress + 0.0);
            return;
        }

        if (CheckProgress > 0.001)
        {
            var gradient = new LinearGradientBrush(
                Color.FromRgb(0x7C, 0xE7, 0xA0),
                Color.FromRgb(0x34, 0xC7, 0x59),
                new Point(0, 1), new Point(1, 0));
            var pen = new Pen(gradient, 4)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            DrawPartial(dc, pen, new[] { new Point(24, 37), new Point(33, 46), new Point(49, 29) }, CheckProgress);
        }
    }

    private static StreamGeometry BuildArc(Point center, double radius, double startDegrees, double sweepDegrees)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        var start = PointOnCircle(center, radius, startDegrees);
        var end = PointOnCircle(center, radius, startDegrees + sweepDegrees);
        ctx.BeginFigure(start, false, false);
        ctx.ArcTo(end, new Size(radius, radius), 0, sweepDegrees > 180,
            SweepDirection.Clockwise, true, false);
        geometry.Freeze();
        return geometry;
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        double rad = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
    }

    private static void DrawPartial(DrawingContext dc, Pen pen, IReadOnlyList<Point> points, double progress)
    {
        double total = 0;
        for (int i = 1; i < points.Count; i++)
            total += (points[i] - points[i - 1]).Length;
        if (total <= 0) return;

        double remaining = total * Math.Clamp(progress, 0, 1);
        for (int i = 1; i < points.Count && remaining > 0; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            var segment = b - a;
            double len = segment.Length;
            if (len <= 0) continue;
            if (remaining >= len)
            {
                dc.DrawLine(pen, a, b);
                remaining -= len;
            }
            else
            {
                var partial = a + segment * (remaining / len);
                dc.DrawLine(pen, a, partial);
                remaining = 0;
            }
        }
    }
}
