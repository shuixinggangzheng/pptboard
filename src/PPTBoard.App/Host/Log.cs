using System.IO;

namespace PPTBoard.App.Host;

public static class Log
{
    private static readonly object Gate = new();
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PPTBoard", "logs");

    public static bool DebugShow =>
        Environment.GetEnvironmentVariable("PPTBOARD_DEBUG_SHOW") == "1";

    public static string FilePath => Path.Combine(Dir, "app.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // 日志永远不能拖垮主流程
        }
    }
}
