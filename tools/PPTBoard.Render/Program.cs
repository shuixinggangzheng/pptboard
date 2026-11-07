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

        if (args.Length > 0 && args[0] == "--selftest")
        {
            int failures = RunSelfTest(layout);
            app.Shutdown();
            Environment.ExitCode = failures == 0 ? 0 : 1;
            return;
        }

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

    private static int RunSelfTest(LayoutSpec layout)
    {
        int failures = 0;
        void Check(string name, bool ok)
        {
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
            if (!ok) failures++;
        }

        void Click(Window window, string name)
        {
            if (window.FindName(name) is System.Windows.Controls.Button button)
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            else
                Check("find " + name, false);
        }

        var engine = new InkEngine();
        var dock = new MainDockWindow(layout);
        bool board = false, prev = false, next = false, pen = false, color = false, end = false;
        dock.BoardRequested += (_, _) => board = true;
        dock.PreviousRequested += (_, _) => prev = true;
        dock.NextRequested += (_, _) => next = true;
        dock.PenRequested += (_, _) => pen = true;
        dock.ColorRequested += (_, _) => color = true;
        dock.EndShowRequested += (_, _) => end = true;


        Click(dock, "BoardButton");
        Click(dock, "PrevButton");
        Click(dock, "NextButton");
        Click(dock, "PenButton");
        Click(dock, "ColorButton");
        Click(dock, "EndButton");
        Check("dock board event", board);
        Check("dock prev event", prev);
        Check("dock next event", next);
        Check("dock pen event", pen);
        Check("dock color event", color);
        Check("dock end event", end);

        var penDock = new PenDockWindow(layout, engine);
        Click(penDock, "Preset2");
        Check("pen preset red", engine.Style.Color == InkColor.Red);
        Click(penDock, "Width3");
        Check("pen width 16", Math.Abs(engine.Style.Width - 16) < 0.01);

        var boardWindow = new BoardWindow(layout, engine);
        engine.PointerDown(1, 10, 10);
        engine.PointerMove(1, 120, 80);
        engine.PointerUp(1);
        int before = engine.Document.Current.Strokes.Count;
        Click(boardWindow, "UndoButton");
        Check("board undo removes stroke", engine.Document.Current.Strokes.Count == before - 1);

        Click(boardWindow, "EraserButton");
        Check("board eraser toggles tool", engine.Tool == InkTool.Eraser);

        engine.Document.AddPageAfterCurrent();
        var menu = new PageMenuWindow(layout, engine);
        menu.Prepare();
        var items = menu.FindName("ItemsPanel") as System.Windows.Controls.WrapPanel;
        Check("page menu item count", items is not null && items.Children.Count == engine.Document.PageCount);

        Console.WriteLine("SELFTEST " + (failures == 0 ? "PASS" : "FAIL") + " failures=" + failures);
        return failures;
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
