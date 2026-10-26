
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MacTopBar;

public sealed record MonitorInfo(
    int Left,
    int Top,
    int Width,
    int Height,
    int DpiX = 96,
    int DpiY = 96)
{
    // Calculate the right edge of this monitor.
    public int Right => Left + Width;

    // Calculate the bottom edge of this monitor.
    public int Bottom => Top + Height;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool MonitorEnumProc(
        IntPtr monitor,
        IntPtr hdc,
        ref RECT rect,
        IntPtr data);

    private static readonly MonitorEnumProc Callback = MonitorCallback;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr clipRect,
        MonitorEnumProc callback,
        IntPtr data);

    [DllImport("shcore.dll", SetLastError = true)]
    private static extern int GetDpiForMonitor(
        IntPtr hMonitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private static bool MonitorCallback(
        IntPtr monitor,
        IntPtr hdc,
        ref RECT rect,
        IntPtr data)
    {
        try
        {
            var handle = GCHandle.FromIntPtr(data);

            if (handle.Target is List<MonitorInfo> monitors)
            {
                int dpiX = 96;
                int dpiY = 96;

                try
                {
                    if (GetDpiForMonitor(
                        monitor, 0, out var x, out var y) == 0
                        && x > 0 && y > 0)
                    {
                        dpiX = (int)x;
                        dpiY = (int)y;
                    }
                }
                catch
                {
                    // Keep the default 96 DPI if unavailable.
                }

                monitors.Add(new MonitorInfo(
                    rect.Left,
                    rect.Top,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top,
                    dpiX,
                    dpiY));
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    internal static IReadOnlyList<MonitorInfo> GetAll()
    {
        var monitors = new List<MonitorInfo>();
        var handle = GCHandle.Alloc(monitors);

        try
        {
            EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                Callback,
                GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }

        if (monitors.Count == 0)
        {
            monitors.Add(new MonitorInfo(
                0,
                0,
                Math.Max(1, GetSystemMetrics(0)),
                Math.Max(1, GetSystemMetrics(1))));
        }

        return monitors;
    }
}
