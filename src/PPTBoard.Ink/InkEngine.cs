using PPTBoard.Core.Geometry;
using PPTBoard.Core.Models;

namespace PPTBoard.Ink;

public enum InkTool
{
    Pen,
    Eraser
}

/// <summary>
/// 多指墨迹引擎：每个 pointerId 一条独立笔迹；整笔擦除；每页独立撤销。
/// 不依赖 WPF / Win32，可在单元测试里直接驱动。
/// </summary>
public sealed class InkEngine
{
    private readonly Dictionary<int, InkStroke> _activeStrokes = new();
    private readonly Dictionary<int, RemoveStrokesAction> _eraserGestures = new();
    private long _nextStrokeId = 1;

    public InkEngine(InkDocument? document = null)
    {
        Document = document ?? new InkDocument();
    }

    public InkDocument Document { get; }
    public InkTool Tool { get; private set; } = InkTool.Pen;
    public PenStyle Style { get; private set; } = PenStyle.Default;
    public double EraserRadius { get; set; } = 16;

    public IEnumerable<InkStroke> ActiveStrokes => _activeStrokes.Values;

    public void SetTool(InkTool tool) => Tool = tool;

    public void SetStyle(PenStyle style) => Style = style.Clamp();

    public bool PointerDown(int pointerId, double x, double y)
    {
        if (_activeStrokes.ContainsKey(pointerId) || _eraserGestures.ContainsKey(pointerId))
            return false;

        var page = Document.Current;
        if (Tool == InkTool.Eraser)
        {
            var gesture = new RemoveStrokesAction();
            _eraserGestures[pointerId] = gesture;
            EraseAt(pointerId, gesture, page, x, y);
            return true;
        }

        var stroke = new InkStroke(_nextStrokeId++, pointerId, Style.Color, Style.Width, new InkPoint(x, y));
        page.Add(stroke);
        _activeStrokes[pointerId] = stroke;
        Document.RaiseInvalidated(stroke.Bounds);
        return true;
    }

    public bool PointerMove(int pointerId, double x, double y)
    {
        if (_activeStrokes.TryGetValue(pointerId, out var stroke))
        {
            var before = stroke.Bounds;
            stroke.AddPoint(new InkPoint(x, y));
            Document.RaiseInvalidated(RectD.Union(before, stroke.Bounds));
            return true;
        }

        if (_eraserGestures.TryGetValue(pointerId, out var gesture))
        {
            EraseAt(pointerId, gesture, Document.Current, x, y);
            return true;
        }

        return false;
    }

    public bool PointerUp(int pointerId)
    {
        if (_activeStrokes.Remove(pointerId, out var stroke))
        {
            stroke.Complete();
            Document.Current.History.Push(new AddStrokeAction(stroke));
            Document.RaiseContentChanged();
            return true;
        }

        if (_eraserGestures.Remove(pointerId, out var gesture))
        {
            if (gesture.HasRemovals)
            {
                Document.Current.History.Push(gesture);
                Document.RaiseContentChanged();
            }
            return true;
        }

        return false;
    }

    /// <summary>指针丢失：已画的保留但结束，撤销步照常压栈；擦除手势按已擦除提交。</summary>
    public bool PointerCancel(int pointerId) => PointerUp(pointerId);

    public bool Undo()
    {
        var page = Document.Current;
        var dirty = page.History.Undo(page);
        if (dirty is null) return false;
        Document.RaiseInvalidated(dirty.Value);
        Document.RaiseContentChanged();
        return true;
    }

    public bool ClearCurrentPage()
    {
        var page = Document.Current;
        if (page.IsEmpty) return false;

        RectD dirty = RectD.Empty;
        foreach (var stroke in page.Strokes)
            dirty = RectD.Union(dirty, stroke.Bounds);

        var snapshot = page.Strokes.ToArray();
        page.Clear();
        page.History.Push(new ClearPageAction(snapshot));

        Document.RaiseInvalidated(dirty);
        Document.RaiseContentChanged();
        return true;
    }

    /// <summary>切页/关板/退出放映前调用，清掉进行中的指针状态。</summary>
    public void CancelActivePointers()
    {
        foreach (var stroke in _activeStrokes.Values)
            stroke.Complete();
        _activeStrokes.Clear();
        _eraserGestures.Clear();
    }

    private void EraseAt(int pointerId, RemoveStrokesAction gesture, InkPage page, double x, double y)
    {
        RectD dirty = RectD.Empty;
        for (int i = page.Strokes.Count - 1; i >= 0; i--)
        {
            var stroke = page.Strokes[i];
            if (!stroke.Bounds.Inflate(EraserRadius).Contains(x, y))
                continue;
            if (!HitTest(stroke, x, y))
                continue;

            page.RemoveAt(i);
            gesture.Add(i, stroke);
            dirty = RectD.Union(dirty, stroke.Bounds);
        }

        if (!dirty.IsEmpty)
            Document.RaiseInvalidated(dirty);
    }

    private bool HitTest(InkStroke stroke, double x, double y)
    {
        double tolerance = stroke.Width / 2 + EraserRadius;
        double toleranceSq = tolerance * tolerance;
        var points = stroke.Points;

        if (points.Count == 1)
            return DistanceSquared(points[0].X, points[0].Y, x, y) <= toleranceSq;

        for (int i = 1; i < points.Count; i++)
        {
            if (DistanceToSegmentSquared(points[i - 1], points[i], x, y) <= toleranceSq)
                return true;
        }
        return false;
    }

    private static double DistanceSquared(double x1, double y1, double x2, double y2)
    {
        double dx = x1 - x2;
        double dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static double DistanceToSegmentSquared(InkPoint a, InkPoint b, double x, double y)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        if (dx == 0 && dy == 0) return DistanceSquared(a.X, a.Y, x, y);

        double t = ((x - a.X) * dx + (y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);
        double px = a.X + t * dx;
        double py = a.Y + t * dy;
        return DistanceSquared(px, py, x, y);
    }
}
