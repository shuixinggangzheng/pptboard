using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace PPTBoard.Wps;

internal static class ComInterop
{
    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string progId, out Guid clsid);

    [DllImport("oleaut32.dll")]
    private static extern int GetActiveObject(ref Guid clsid, IntPtr reserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object? obj);

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx context);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CreateFileMoniker(string path, out IMoniker moniker);

    public static object? TryGetActiveObject(string progId)
    {
        try
        {
            int hr = CLSIDFromProgID(progId, out var clsid);
            if (hr != 0) return null;
            hr = GetActiveObject(ref clsid, IntPtr.Zero, out var obj);
            return hr == 0 ? obj : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>找不到 KWPP.Application 时，从 ROT 里找已打开的 pptx 文档 moniker。</summary>
    public static object? TryGetPresentationFromRot()
    {
        var list = GetPresentationsFromRot();
        return list.Count > 0 ? list[0] : null;
    }

    /// <summary>ROT 里所有已打开演示文稿的 moniker 对象。</summary>
    public static List<object> GetPresentationsFromRot()
    {
        var result = new List<object>();
        try
        {
            if (GetRunningObjectTable(0, out var table) != 0) return result;
            if (CreateBindCtx(0, out var context) != 0) return result;

            table.EnumRunning(out var enumerator);
            var monikers = new IMoniker[1];
            IntPtr fetched = IntPtr.Zero;

            while (enumerator.Next(1, monikers, fetched) == 0)
            {
                try
                {
                    monikers[0].GetDisplayName(context, null, out string name);
                    if (!IsPresentationPath(name)) continue;

                    table.GetObject(monikers[0], out object presentation);
                    if (presentation is not null) result.Add(presentation);
                }
                catch
                {
                    // 单个 moniker 失败不影响继续找
                }
            }
        }
        catch
        {
            // ROT 不可用时返回空
        }

        return result;
    }

    public static object? GetByPath(string path)
    {
        try
        {
            if (GetRunningObjectTable(0, out var table) != 0) return null;
            if (CreateFileMoniker(path, out var moniker) != 0) return null;
            table.GetObject(moniker, out object obj);
            return obj;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPresentationPath(string name)
    {
        return name.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".ppt", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".dps", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".dpsx", StringComparison.OrdinalIgnoreCase);
    }
}
