namespace PPTBoard.Core.Models;

/// <summary>ARGB 颜色，不依赖任何 UI 框架。</summary>
public readonly record struct InkColor(byte A, byte R, byte G, byte B)
{
    public static readonly InkColor Black = new(255, 0, 0, 0);
    public static readonly InkColor White = new(255, 255, 255, 255);
    public static readonly InkColor Red = new(255, 229, 57, 53);
    public static readonly InkColor Blue = new(255, 30, 136, 229);
    public static readonly InkColor Yellow = new(255, 253, 216, 53);

    public static readonly InkColor[] Presets = [Black, White, Red, Blue, Yellow];

    public uint ToArgb() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

    public static InkColor FromArgb(uint argb) =>
        new((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    public InkColor WithAlpha(byte alpha) => this with { A = alpha };

    /// <summary>把色相按明度滑杆压暗或提亮：0=黑，0.5=原色，1=白。</summary>
    public InkColor WithBrightness(double brightness)
    {
        double b = Math.Clamp(brightness, 0, 1);
        byte Mix(byte channel)
        {
            double v = channel / 255.0;
            double result = b < 0.5 ? v * (b * 2) : v + (1 - v) * ((b - 0.5) * 2);
            return (byte)Math.Round(Math.Clamp(result, 0, 1) * 255);
        }

        return this with { R = Mix(R), G = Mix(G), B = Mix(B) };
    }

    public string ToHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
}
