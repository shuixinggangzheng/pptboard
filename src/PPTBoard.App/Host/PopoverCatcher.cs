using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PPTBoard.Core.Geometry;

namespace PPTBoard.App.Host;

/// <summary>颜色面板打开时铺满屏幕的透明层：点面板外任意位置关闭面板。</summary>
public sealed class PopoverCatcher : OverlayWindow
{
    public event EventHandler? Pressed;

    public PopoverCatcher(LayoutSpec layout)
    {
        Left = 0;
        Top = 0;
        Width = layout.ScreenWidth;
        Height = layout.ScreenHeight;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        Focusable = false;
        MouseLeftButtonDown += (_, _) => Pressed?.Invoke(this, EventArgs.Empty);
    }
}
