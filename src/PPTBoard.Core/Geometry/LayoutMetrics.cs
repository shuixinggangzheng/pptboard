namespace PPTBoard.Core.Geometry;

/// <summary>一套屏幕上的全部布局矩形（DIP）。窗口层不得写死像素，一律从这里取。</summary>
public sealed record LayoutSpec(
    double ScreenWidth,
    double ScreenHeight,
    double SideMargin,
    double BottomMargin,
    RectD Board,
    RectD GrayBar,
    RectD PageBox,
    RectD PenDock,
    RectD MainDock,
    RectD PageMenu,
    RectD ClearSlider,
    RectD ColorPanel,
    RectD LoadingDisc)
{
    public const double DockHeight = 40;
    public const double PenDockHeight = 56;
    public const double GrayBarHeight = 56;
    public const double PageMenuWidth = 320;
    public const double MainDockWidth = 332;
    public const double LoadingDiscSize = 72;
    public const double ScreenEdgeGap = 32;

    /// <summary>画板内真正可绘制的区域（灰条以上）。</summary>
    public RectD DrawingArea => new(Board.X, Board.Y, Board.Width, GrayBar.Y - Board.Y);

    /// <summary>主 dock 里颜色圆点的中心 X（用于定位颜色面板）。</summary>
    public double ColorDotCenterX => MainDock.X + 14 + 20;

    public static LayoutSpec Compute(double screenWidth, double screenHeight)
    {
        if (screenWidth < 800 || screenHeight < 600)
            throw new ArgumentOutOfRangeException(nameof(screenWidth), "屏幕尺寸不合理");

        double side = Math.Round(screenWidth / 15.0);
        double bottom = Math.Max(Math.Round(screenHeight / 15.0), DockHeight + PenDockHeight);
        double boardWidth = screenWidth - side * 2;
        double boardHeight = screenHeight - bottom;

        var board = new RectD(side, 0, boardWidth, boardHeight);
        var grayBar = new RectD(board.X, board.Bottom - GrayBarHeight, board.Width, GrayBarHeight);

        double pageBoxWidth = Math.Round(board.Width / 15.0);
        var pageBox = new RectD(grayBar.Right - pageBoxWidth, grayBar.Y, pageBoxWidth, GrayBarHeight);

        double penDockWidth = Math.Round(board.Width * 0.40);
        var penDock = new RectD(
            Math.Round((screenWidth - penDockWidth) / 2.0),
            board.Bottom,
            penDockWidth,
            PenDockHeight);

        var mainDock = new RectD(
            Math.Round((screenWidth - MainDockWidth) / 2.0),
            screenHeight - DockHeight,
            MainDockWidth,
            DockHeight);

        var pageMenu = new RectD(screenWidth - PageMenuWidth, 0, PageMenuWidth, screenHeight);

        var clearSlider = new RectD(
            Math.Round((screenWidth - 400) / 2.0),
            grayBar.Y - 12 - 44,
            400,
            44);

        const double panelWidth = 340;
        const double panelHeight = 420;
        double dotCenterX = mainDock.X + 14 + 20;
        double panelX = Math.Clamp(dotCenterX - panelWidth / 2, 12, screenWidth - panelWidth - 12);
        double panelY = Math.Max(12, mainDock.Y - 12 - panelHeight);
        var colorPanel = new RectD(panelX, panelY, panelWidth, panelHeight);

        var loadingDisc = new RectD(
            screenWidth - ScreenEdgeGap - LoadingDiscSize,
            screenHeight - ScreenEdgeGap - LoadingDiscSize,
            LoadingDiscSize,
            LoadingDiscSize);

        return new LayoutSpec(
            screenWidth, screenHeight, side, bottom,
            board, grayBar, pageBox, penDock, mainDock,
            pageMenu, clearSlider, colorPanel, loadingDisc);
    }
}
