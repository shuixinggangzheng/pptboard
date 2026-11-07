namespace PPTBoard.Core.Geometry;

/// <summary>与 UI 框架无关的矩形（DIP）。</summary>
public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public static readonly RectD Empty = new(0, 0, 0, 0);

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(double x, double y) =>
        x >= X && x < Right && y >= Y && y < Bottom;

    public RectD Inflate(double amount) =>
        new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    public static RectD Union(RectD a, RectD b)
    {
        if (a.IsEmpty) return b;
        if (b.IsEmpty) return a;
        double left = Math.Min(a.Left, b.Left);
        double top = Math.Min(a.Top, b.Top);
        double right = Math.Max(a.Right, b.Right);
        double bottom = Math.Max(a.Bottom, b.Bottom);
        return new(left, top, right - left, bottom - top);
    }

    public static RectD FromPoints(double x1, double y1, double x2, double y2) =>
        new(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));

    public override string ToString() =>
        $"({X:0.#},{Y:0.#},{Width:0.#}x{Height:0.#})";
}
