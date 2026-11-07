using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PPTBoard.App.Board;
using PPTBoard.App.Boot;
using PPTBoard.App.Dock;
using PPTBoard.Core.Geometry;
using PPTBoard.Core.Models;
using PPTBoard.Ink;

namespace PPTBoard.Render;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--com")
        {
            var bridge = new PPTBoard.Wps.WpsBridge();
            bool attached = bridge.TryAttach();
            Console.WriteLine($"attached={attached} available={bridge.IsAvailable} slides={bridge.SlideCount} show={bridge.IsShowRunning} canNavigate={bridge.CanNavigate} pen={bridge.NativePenSupported} pos={bridge.CurrentPosition}");
            return;
        }

        string outDir = args.Length > 0 ? args[0] : "render";
        Directory.CreateDirectory(outDir);

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/PPTBoard;component/Theme/Styles.xaml")
        });

        var layout = LayoutSpec.Compute(1920, 1080);
        var engine = BuildInk();

        RenderContent(new MainDockWindow(layout), 288, 40, Path.Combine(outDir, "dock.png"));
        RenderContent(new PenDockWindow(layout, engine), 666, 56, Path.Combine(outDir, "pen-dock.png"));
        RenderContent(new BoardWindow(layout, engine), 1664, 984, Path.Combine(outDir, "board.png"));
        RenderContent(new ColorPanelWindow(layout), 340, 420, Path.Combine(outDir, "color-panel.png"));
        RenderContent(new ClearSliderWindow(layout), 400, 44, Path.Combine(outDir, "clear-slider.png"));

        var menu = new PageMenuWindow(layout, engine);
        menu.Prepare();
        RenderContent(menu, 320, 1080, Path.Combine(outDir, "page-menu.png"));

        var loading = new LoadingDiscView
        {
            Width = 72,
            Height = 72,
            Progress = 0.95,
            CheckProgress = 1
        };
        RenderElement(loading, 72, 72, Path.Combine(outDir, "loading.png"));

        Console.WriteLine("rendered to " + Path.GetFullPath(outDir));
        app.Shutdown();
    }

    private static InkEngine BuildInk()
    {
        var engine = new InkEngine();

        engine.SetStyle(new PenStyle(InkColor.Black, 6));
        Stroke(engine, 1, new[] { (300, 260), (520, 240), (760, 300), (980, 360) });

        engine.SetStyle(new PenStyle(InkColor.Red, 10));
        Stroke(engine, 2, new[] { (320, 460), (560, 520), (820, 470), (1050, 560) });

        engine.SetStyle(new PenStyle(InkColor.Blue, 16));
        Stroke(engine, 3, new[] { (340, 660), (620, 700), (900, 640), (1120, 720) });

        engine.Document.AddPageAfterCurrent();
        engine.SetStyle(new PenStyle(InkColor.Black, 6));
        Stroke(engine, 4, new[] { (300, 300), (800, 500), (1200, 380) });
        engine.Document.GoTo(0);

        return engine;
    }

    private static void Stroke(InkEngine engine, int pointerId, (int X, int Y)[] points)
    {
        engine.PointerDown(pointerId, points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++)
            engine.PointerMove(pointerId, points[i].X, points[i].Y);
        engine.PointerUp(pointerId);
    }

    private static void RenderContent(Window window, int width, int height, string path)
    {
        var content = (FrameworkElement)window.Content;
        RenderElement(content, width, height, path);
    }

    private static void RenderElement(FrameworkElement element, int width, int height, string path)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
