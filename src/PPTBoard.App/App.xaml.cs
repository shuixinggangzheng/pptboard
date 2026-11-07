using System.Threading;
using System.Windows;
using PPTBoard.App.Boot;
using PPTBoard.App.Dock;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;
using PPTBoard.Ink;

namespace PPTBoard.App;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private SlideshowHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 没有放映时不创建任何窗口，也不显示 loading；显式控制退出
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _singleInstance = new Mutex(true, @"Local\PPTBoard.App.SingleInstance", out bool created);
        if (!created)
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception.ToString());
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Error("domain: " + args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) => Log.Error("task: " + args.Exception);

        Log.Info("OnStartup begin");

        var screen = Interop.NativeMethods.GetPrimaryScreenDip();
        Log.Info($"screen dip={screen.Width}x{screen.Height} dpi={Interop.NativeMethods.GetDpiForSystem()} system={SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}");
        var layout = LayoutSpec.Compute(screen.Width, screen.Height);
        Log.Info($"layout screen={layout.ScreenWidth}x{layout.ScreenHeight} debug={Log.DebugShow} dock={layout.MainDock}");

        var ink = new InkEngine();
        var bridge = Environment.GetEnvironmentVariable("PPTBOARD_DEBUG_FAKE_SHOW") == "1"
            ? (PPTBoard.Core.Contracts.ISlideshowBridge)new FakeSlideshowBridge()
            : new PPTBoard.Wps.WpsBridge();
        _host = new SlideshowHost(layout, ink, bridge);

        bool fakeShow = Environment.GetEnvironmentVariable("PPTBOARD_DEBUG_FAKE_SHOW") == "1";
        bool showNow = Log.DebugShow || fakeShow || bridge.IsShowRunning || PPTBoard.Wps.ShowWindowProbe.IsRunning();
        Log.Info($"startup show active={showNow}");

        if (!showNow)
        {
            Log.Info("no show at startup; boot without loader");
            _ = BootWithoutLoaderAsync();
            return;
        }

        var loader = new LoadingWindow(layout);
        Log.Info("loader created, showing");
        loader.Show();
        Log.Info("loader shown");
        _ = BootAsync(loader);
    }

    private async Task BootAsync(LoadingWindow loader)
    {
        try
        {
            Log.Info("boot start");
            var init = _host!.InitializeAsync();
            Log.Info("init task created");
            await loader.RingTo95Async();
            Log.Info("ring done");

            bool ok;
            try
            {
                ok = await init.WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch
            {
                ok = false;
            }

            Log.Info($"init result={ok}");
            await loader.CompleteAsync(ok);
            Log.Info("loader animation done");
            loader.Close();
            Log.Info("loader closed");

            if (ok)
            {
                _host.Start();
                Log.Info("host started");
            }
            else
            {
                Log.Info("bridge init failed, exiting");
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            Log.Error("boot: " + ex);
            Shutdown();
        }
    }

    private async Task BootWithoutLoaderAsync()
    {
        try
        {
            Log.Info("boot start (no loader)");
            var init = _host!.InitializeAsync();
            bool ok;
            try
            {
                ok = await init.WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch
            {
                ok = false;
            }

            Log.Info($"init result={ok} (no loader)");
            if (ok)
            {
                _host.Start();
                Log.Info("host started (no loader)");
            }
            else
            {
                Log.Info("bridge init failed, exiting");
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            Log.Error("boot-no-loader: " + ex);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
