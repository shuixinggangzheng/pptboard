using PPTBoard.Core.Geometry;
using Xunit;

namespace PPTBoard.Core.Tests;

public class LayoutMetricsTests
{
    [Fact]
    public void FullHd_MatchesSpec()
    {
        var l = LayoutSpec.Compute(1920, 1080);

        Assert.Equal(128, l.SideMargin);
        Assert.Equal(96, l.BottomMargin);
        Assert.Equal(new RectD(128, 0, 1664, 984), l.Board);
        Assert.Equal(new RectD(128, 928, 1664, 56), l.GrayBar);
        Assert.Equal(new RectD(1681, 928, 111, 56), l.PageBox);
        Assert.Equal(new RectD(627, 984, 666, 56), l.PenDock);
        Assert.Equal(new RectD(816, 1040, 288, 40), l.MainDock);
        Assert.Equal(new RectD(1600, 0, 320, 1080), l.PageMenu);
        Assert.Equal(new RectD(760, 872, 400, 44), l.ClearSlider);
        Assert.Equal(new RectD(680, 608, 340, 420), l.ColorPanel);
        Assert.Equal(new RectD(1816, 976, 72, 72), l.LoadingDisc);
        Assert.Equal(new RectD(128, 0, 1664, 928), l.DrawingArea);
    }

    [Fact]
    public void SmallScreen_KeepsDockStack()
    {
        var l = LayoutSpec.Compute(1536, 864);

        Assert.True(l.Board.Width > 0);
        Assert.True(l.Board.Height > 0);
        Assert.Equal(96, l.BottomMargin);
        Assert.Equal(l.Board.Bottom, l.GrayBar.Bottom);
        Assert.Equal(l.Board.Bottom, l.PenDock.Top);
        Assert.Equal(l.ScreenHeight, l.MainDock.Bottom);
        Assert.Equal(l.Board.Width, l.GrayBar.Width);
    }

    [Fact]
    public void MainDock_IsCentered()
    {
        var l = LayoutSpec.Compute(1920, 1080);
        Assert.Equal(l.ScreenWidth / 2, l.MainDock.CenterX, 3);
        Assert.Equal(l.ScreenWidth / 2, l.PenDock.CenterX, 3);
        Assert.Equal(l.ScreenWidth / 2, l.GrayBar.CenterX, 3);
    }

    [Fact]
    public void ColorPanel_StaysOnScreen()
    {
        var l = LayoutSpec.Compute(1920, 1080);
        Assert.True(l.ColorPanel.Left >= 0);
        Assert.True(l.ColorPanel.Right <= l.ScreenWidth);
        Assert.True(l.ColorPanel.Top >= 0);
        Assert.True(l.ColorPanel.Bottom <= l.MainDock.Top);
    }
}
