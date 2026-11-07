namespace PPTBoard.Core.Models;

/// <summary>一支笔在落笔瞬间的样子。</summary>
public readonly record struct PenStyle(InkColor Color, double Width)
{
    public const double MinWidth = 1;
    public const double MaxWidth = 32;
    public static readonly double[] WidthPresets = [3, 6, 10, 16, 24];

    public static readonly PenStyle Default = new(InkColor.Black, 6);

    public PenStyle Clamp() => this with { Width = Math.Clamp(Width, MinWidth, MaxWidth) };
}
