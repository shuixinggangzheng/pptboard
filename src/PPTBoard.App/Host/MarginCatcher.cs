using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PPTBoard.Core.Geometry;

namespace PPTBoard.App.Host;

/// <summary>画板左右留白上的透明点击层：点留白 = 收起画板。不覆盖画板，避免层级互压。</summary>
public sealed class MarginCatcher : OverlayWindow
{
    public event EventHandler? Pressed;

    public MarginCatcher(RectD rect)
    {
        Left = rect.X;
        Top = rect.Y;
        Width = rect.Width;
        Height = rect.Height;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        Focusable = false;
        MouseLeftButtonDown += (_, _) => Pressed?.Invoke(this, EventArgs.Empty);
    }
}
