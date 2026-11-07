using System.Runtime.InteropServices;

namespace PPTBoard.App.Interop;

/// <summary>发送 WPS / PowerPoint 放映快捷键。WPS 的橡皮没有 COM 指针类型，只能用 Ctrl+E。</summary>
internal static class KeyboardInput
{
    private const byte VK_CONTROL = 0x11;
    private const byte VK_E = 0x45;
    private const byte VK_A = 0x41;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

    public static void SendEraser() => SendCtrl(VK_E);
    public static void SendArrow() => SendCtrl(VK_A);

    private static void SendCtrl(byte key)
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, 2, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, 2, UIntPtr.Zero);
    }
}
