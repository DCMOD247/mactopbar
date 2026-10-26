using System.Runtime.InteropServices;
using System.Text;

namespace MacTopBar;

internal static class NativeMethods
{
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;
    internal const int WS_EX_NOACTIVATE = 0x08000000;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_SHOWWINDOW = 0x0040;
    internal const uint SWP_FRAMECHANGED = 0x0020;
    internal const int SW_MINIMIZE = 6;
    internal const int SW_RESTORE = 9;
    internal const int WM_CLOSE = 0x0010;
    internal const int WM_MOUSEACTIVATE = 0x0021;
    internal const uint ABM_NEW = 0x00000000;
    internal const uint ABM_REMOVE = 0x00000001;
    internal const uint ABM_QUERYPOS = 0x00000002;
    internal const uint ABM_SETPOS = 0x00000003;
    internal const uint ABE_TOP = 1;
    internal const int WM_APPBAR_CALLBACK = 0x8001;
    internal const int ABN_POSCHANGED = 3;

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool IsZoomed(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)] internal static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)] internal static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] internal static extern uint GetDpiForSystem();
    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)] internal static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct APPBARDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value)
    {
        return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, value) : new IntPtr(SetWindowLong32(hWnd, nIndex, value.ToInt32()));
    }

    // WLAN native API: used to report a real Wi-Fi interface state rather than an assumed icon.
    [DllImport("wlanapi.dll", SetLastError = true)] internal static extern int WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr clientHandle);
    [DllImport("wlanapi.dll", SetLastError = true)] internal static extern int WlanEnumInterfaces(IntPtr clientHandle, IntPtr reserved, out IntPtr interfaceList);
    [DllImport("wlanapi.dll")] internal static extern void WlanFreeMemory(IntPtr memory);
    [DllImport("wlanapi.dll")] internal static extern int WlanCloseHandle(IntPtr clientHandle, IntPtr reserved);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WLAN_INTERFACE_INFO
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string InterfaceDescription;
        public int State;
    }

    // Acrylic/blur composition API. If unavailable, the bar retains its translucent solid background.
    [StructLayout(LayoutKind.Sequential)]
    internal struct ACCENT_POLICY { public int AccentState; public int AccentFlags; public int GradientColor; public int AnimationId; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWCOMPOSITIONATTRIBDATA { public int Attribute; public IntPtr Data; public int SizeOfData; }
    [DllImport("user32.dll", EntryPoint = "SetWindowCompositionAttribute")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hWnd, ref WINDOWCOMPOSITIONATTRIBDATA data);

    internal static string GetWifiState()
    {
        IntPtr client = IntPtr.Zero, list = IntPtr.Zero;
        try
        {
            if (WlanOpenHandle(2, IntPtr.Zero, out _, out client) != 0) return "Wi-Fi status unavailable";
            if (WlanEnumInterfaces(client, IntPtr.Zero, out list) != 0 || list == IntPtr.Zero) return "No Wi-Fi interface found";
            int count = Marshal.ReadInt32(list);
            if (count <= 0) return "No Wi-Fi adapter found";
            int offset = 8; // WLAN_INTERFACE_INFO_LIST has a two-DWORD header followed by a flexible array.
            int stride = Marshal.SizeOf<WLAN_INTERFACE_INFO>();
            bool connected = false, disconnected = false, disabled = false;
            for (int i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(IntPtr.Add(list, offset + i * stride));
                if (info.State == 1) connected = true;       // wlan_interface_state_connected
                else if (info.State == 4) disconnected = true; // wlan_interface_state_disconnected
                else disabled = true;
            }
            if (connected) return "Wi-Fi connected";
            if (disconnected) return "Wi-Fi disconnected";
            return disabled ? "Wi-Fi adapter disabled or unavailable" : "Wi-Fi state unavailable";
        }
        catch (DllNotFoundException) { return "Wi-Fi status unavailable"; }
        catch { return "Wi-Fi status unavailable"; }
        finally
        {
            if (list != IntPtr.Zero) WlanFreeMemory(list);
            if (client != IntPtr.Zero) WlanCloseHandle(client, IntPtr.Zero);
        }
    }

    internal static string GetNetworkState()
    {
        try
        {
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() ? "Network available" : "No network connection";
        }
        catch { return "Network status unavailable"; }
    }

    internal static string GetBatteryState()
    {
        if (!GetSystemPowerStatus(out var p)) return "Battery status unavailable";
        if (p.BatteryFlag == 128 || p.BatteryLifePercent == 255) return "No battery detected";
        var percent = $"{p.BatteryLifePercent}%";
        if (p.ACLineStatus == 1) return $"Battery {percent} • Plugged in / charging";
        if ((p.BatteryFlag & 8) != 0) return $"Battery {percent} • Charging state reported";
        return $"Battery {percent} • On battery";
    }
}
