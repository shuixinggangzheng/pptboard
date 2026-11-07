using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PPTBoard.Ink;

namespace PPTBoard.App.Board;

/// <summary>把一页白板笔迹渲染成缩略图（与窗口无关，纯 DrawingVisual）。</summary>
public static class PageThumbnail
{
    public static BitmapSource Render(InkPage page, double sourceWidth, double sourceHeight, int pixelWidth = 264, int pixelHeight = 148)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pixelWidth, pixelHeight));
            if (sourceWidth <= 0 || sourceHeight <= 0) { }

            double scale = Math.Min(pixelWidth / Math.Max(1, sourceWidth), pixelHeight / Math.Max(1, sourceHeight));
            foreach (var stroke in page.Strokes)
            {
                var points = stroke.Points;
                if (points.Count == 0) continue;

                var color = Color.FromArgb(stroke.Color.A, stroke.Color.R, stroke.Color.G, stroke.Color.B);
                var brush = new SolidColorBrush(color);
                var pen = new Pen(brush, Math.Max(1, stroke.Width * scale))
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };

                if (points.Count == 1)
                {
                    dc.DrawEllipse(brush, null,
                        new Point(points[0].X * scale, points[0].Y * scale),
                        stroke.Width * scale / 2, stroke.Width * scale / 2);
                    continue;
                }

                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(new Point(points[0].X * scale, points[0].Y * scale), false, false);
                    for (int i = 1; i < points.Count; i++)
                        ctx.LineTo(new Point(points[i].X * scale, points[i].Y * scale), true, false);
                }
                geometry.Freeze();
                dc.DrawGeometry(null, pen, geometry);
            }
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
