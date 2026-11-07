using PPTBoard.Core.Geometry;
using PPTBoard.Core.Models;

namespace PPTBoard.Ink;

public readonly record struct InkPoint(double X, double Y);

/// <summary>一条笔迹：落笔时固化颜色与粗细，之后追加点。</summary>
public sealed class InkStroke
{
    private readonly List<InkPoint> _points = new();

    internal InkStroke(long id, int pointerId, InkColor color, double width, InkPoint first)
    {
        Id = id;
        PointerId = pointerId;
        Color = color;
        Width = Math.Clamp(width, PenStyle.MinWidth, PenStyle.MaxWidth);
        _points.Add(first);
        Bounds = new RectD(first.X - Width / 2, first.Y - Width / 2, Width, Width);
    }

    public long Id { get; }
    public int PointerId { get; }
    public InkColor Color { get; }
    public double Width { get; }
    public IReadOnlyList<InkPoint> Points => _points;
    public bool IsActive { get; internal set; } = true;
    public RectD Bounds { get; private set; }

    internal void AddPoint(InkPoint point)
    {
        _points.Add(point);
        double left = Math.Min(Bounds.Left, point.X - Width / 2);
        double top = Math.Min(Bounds.Top, point.Y - Width / 2);
        double right = Math.Max(Bounds.Right, point.X + Width / 2);
        double bottom = Math.Max(Bounds.Bottom, point.Y + Width / 2);
        Bounds = new RectD(left, top, right - left, bottom - top);
    }

    internal void Complete() => IsActive = false;
}
