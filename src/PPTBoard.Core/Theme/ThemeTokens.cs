namespace PPTBoard.Core.Theme;

/// <summary>视觉常量，ARGB（0xAARRGGBB）。与 WPF 无关。</summary>
public static class ThemeTokens
{
    // 深色玻璃，58% 不透明
    public const uint GlassBackground = 0x9412141A;
    public const uint GlassBorder = 0x24FFFFFF;
    public const uint IconPrimary = 0xEBFFFFFF;
    public const uint IconMuted = 0x8CFFFFFF;

    public const uint Board = 0xFFFFFFFF;
    public const uint GrayBar = 0xFFECECF0;
    public const uint GrayIcon = 0xFF3A3F4B;
    public const uint PageBoxBackground = 0xEBFFFFFF;

    public const uint Accent = 0xFF2F7CF6;
    public const uint Success = 0xFF34C759;
    public const uint Danger = 0xFFE5484D;

    public const double CornerRadius = 10;
    public const double IconSize = 20;
    public const double DockButtonSize = 40;
    public const double DockButtonGap = 4;
    public const double DockPadding = 14;

    public static readonly TimeSpan BoardSlideIn = TimeSpan.FromMilliseconds(260);
    public static readonly TimeSpan BoardSlideOut = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan DockFade = TimeSpan.FromMilliseconds(180);
    public static readonly TimeSpan MenuSlide = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan PressScale = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan LoadingRing = TimeSpan.FromMilliseconds(900);
    public static readonly TimeSpan LoadingCheck = TimeSpan.FromMilliseconds(300);
}
