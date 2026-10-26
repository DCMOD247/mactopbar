using System.Runtime.InteropServices;

namespace MacTopBar;

internal sealed record MonitorInfo(int Left, int Top, int Width, int Height, int DpiX = 96, int DpiY = 96)
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data);

    private static readonly MonitorEnumProc Callback = MonitorCallback;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clipRect, MonitorEnumProc callback, IntPtr data);

    [DllImport("shcore.dll", SetLastError = true)]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private static bool MonitorCallback(IntPtr monitor, IntPtr hdc, ref RECT rect, IntPtr data)
    {
        try
        {
            var handle = GCHandle.FromIntPtr(data);
            if (handle.Target is List<MonitorInfo> monitors)
            {
                int dpiX = 96, dpiY = 96;
                try
                {
                    if (GetDpiForMonitor(monitor, 0, out var x, out var y) == 0 && x > 0 && y > 0)
                    {
                        dpiX = (int)x;
                        dpiY = (int)y;
                    }
                }
                catch { /* 96 DPI is a safe fallback if per-monitor DPI cannot be queried. */ }
                monitors.Add(new MonitorInfo(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, dpiX, dpiY));
            }
        }
        catch { return false; }
        return true;
    }

    internal static IReadOnlyList<MonitorInfo> GetAll()
    {
        var monitors = new List<MonitorInfo>();
        var handle = GCHandle.Alloc(monitors);
        try
        {
            // If enumeration fails, fall back to the primary display dimensions below.
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, GCHandle.ToIntPtr(handle));
        }
        finally { handle.Free(); }

        if (monitors.Count == 0)
            monitors.Add(new MonitorInfo(0, 0, Math.Max(1, GetSystemMetrics(0)), Math.Max(1, GetSystemMetrics(1))));
        return monitors;
    }
}
