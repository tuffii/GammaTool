using System.Runtime.InteropServices;

namespace GammaTool;

internal sealed record MonitorInfo(string DeviceName, string Title, bool IsPrimary)
{
    // ComboBox показывает результат ToString()
    public override string ToString() => Title;
}

internal static class Monitors
{
    public static List<MonitorInfo> List()
    {
        var result = new List<MonitorInfo>();

        for (uint i = 0; ; i++)
        {
            var adapter = NewDevice();
            if (!NativeMethods.EnumDisplayDevices(null, i, ref adapter, 0)) break;
            if ((adapter.StateFlags & NativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0) continue;

            // Второй уровень перечисления даёт имя самого монитора
            var monitor = NewDevice();
            string name = NativeMethods.EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0)
                ? monitor.DeviceString
                : adapter.DeviceString;

            bool primary = (adapter.StateFlags & NativeMethods.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;
            string shortName = adapter.DeviceName.TrimStart('\\', '.');   // "\\.\DISPLAY1" -> "DISPLAY1"
            string title = $"{shortName} — {name}{(primary ? " (основной)" : "")}";

            result.Add(new MonitorInfo(adapter.DeviceName, title, primary));
        }

        return result;
    }

    static NativeMethods.DISPLAY_DEVICE NewDevice()
    {
        var d = new NativeMethods.DISPLAY_DEVICE();
        d.cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>();
        return d;
    }
}
