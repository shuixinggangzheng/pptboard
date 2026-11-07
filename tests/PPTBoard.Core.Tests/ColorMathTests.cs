using PPTBoard.Core.Models;
using Xunit;

namespace PPTBoard.Core.Tests;

public class ColorMathTests
{
    [Fact]
    public void Red_RoundTrips()
    {
        var (h, s, v) = ColorMath.RgbToHsv(255, 0, 0);
        Assert.Equal(0, h, 3);
        Assert.Equal(1, s, 3);
        Assert.Equal(1, v, 3);

        var c = ColorMath.HsvToRgb(0, 1, 1);
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Green_HueIs120()
    {
        var c = ColorMath.HsvToRgb(120, 1, 1);
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Brightness_BlendsToBlackAndWhite()
    {
        var original = new InkColor(255, 200, 100, 50);
        Assert.Equal(original, original.WithBrightness(0.5));

        var black = original.WithBrightness(0);
        Assert.Equal(0, black.R);
        Assert.Equal(0, black.G);
        Assert.Equal(0, black.B);

        var white = original.WithBrightness(1);
        Assert.Equal(255, white.R);
        Assert.Equal(255, white.G);
        Assert.Equal(255, white.B);
    }

    [Fact]
    public void Alpha_IsPreserved()
    {
        var c = new InkColor(255, 10, 20, 30).WithAlpha(128);
        Assert.Equal(128, c.A);
        Assert.Equal(10, c.R);
    }
}
