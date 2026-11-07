using System.Windows;
using System.Windows.Media;
using PPTBoard.Core.Models;
using PPTBoard.Ink;

namespace PPTBoard.App.Board;

/// <summary>把 InkEngine 的笔迹画出来；完成的笔迹进缓存，进行中的每帧重画。</summary>
public sealed class InkLayer : FrameworkElement
{
    private InkEngine? _engine;
    private DrawingGroup? _cache;

    public void Attach(InkEngine engine)
    {
        if (_engine is not null)
        {
            _engine.Document.Invalidated -= OnInvalidated;
            _engine.Document.ContentChanged -= OnContentChanged;
            _engine.Document.CurrentPageChanged -= OnContentChanged;
        }

        _engine = engine;
        _engine.Document.Invalidated += OnInvalidated;
        _engine.Document.ContentChanged += OnContentChanged;
        _engine.Document.CurrentPageChanged += OnContentChanged;
        RebuildCache();
    }

    private void OnInvalidated(Core.Geometry.RectD _) => InvalidateVisual();

    private void OnContentChanged()
    {
        RebuildCache();
        InvalidateVisual();
    }

    private void RebuildCache()
    {
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            if (_engine is not null)
            {
                foreach (var stroke in _engine.Document.Current.Strokes)
                {
                    if (!stroke.IsActive) DrawStroke(dc, stroke);
                }
            }
        }
        group.Freeze();
        _cache = group;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (_engine is null) return;

        if (_cache is null) RebuildCache();
        if (_cache is not null) dc.DrawDrawing(_cache);

        foreach (var stroke in _engine.ActiveStrokes)
            DrawStroke(dc, stroke);
    }

    private static void DrawStroke(DrawingContext dc, InkStroke stroke)
    {
        var color = Color.FromArgb(stroke.Color.A, stroke.Color.R, stroke.Color.G, stroke.Color.B);
        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, stroke.Width)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        var points = stroke.Points;
        if (points.Count == 0) return;
        if (points.Count == 1)
        {
            dc.DrawEllipse(brush, null, new Point(points[0].X, points[0].Y), stroke.Width / 2, stroke.Width / 2);
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(points[0].X, points[0].Y), false, false);
            for (int i = 1; i < points.Count; i++)
                ctx.LineTo(new Point(points[i].X, points[i].Y), true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
