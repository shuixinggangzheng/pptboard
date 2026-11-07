using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using PPTBoard.Core.Geometry;
using PPTBoard.App.Host;

namespace PPTBoard.App.Boot;

public partial class LoadingWindow : OverlayWindow
{
    public LoadingWindow(LayoutSpec layout)
    {
        InitializeComponent();
        Left = layout.LoadingDisc.X;
        Top = layout.LoadingDisc.Y;
    }

    public async Task RingTo95Async()
    {
        await AnimateAsync(0, 0.95, 900, value => Disc.Progress = value);
    }

    public async Task CompleteAsync(bool ok)
    {
        if (!ok)
        {
            Disc.Failed = true;
            await AnimateAsync(0, 1, 250, value => Disc.CheckProgress = value);
            await Task.Delay(2000);
            return;
        }

        await AnimateAsync(Disc.Progress, 1, 120, value => Disc.Progress = value);
        await AnimateAsync(0, 1, 300, value => Disc.CheckProgress = value);
        await Task.Delay(200);
        await FadeOutAsync();
    }

    private async Task FadeOutAsync()
    {
        var animation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        BeginAnimation(OpacityProperty, animation);
        await Task.Delay(220);
    }

    private static async Task AnimateAsync(double from, double to, int milliseconds, Action<double> apply)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < milliseconds)
        {
            double t = sw.ElapsedMilliseconds / (double)milliseconds;
            apply(from + (to - from) * t);
            await Task.Delay(15);
        }
        apply(to);
    }
}
