using Microsoft.Win32;

namespace GammaTool;

internal static class Autostart
{
    const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "GammaTool";

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        if (key is null) return;

        if (enabled && Environment.ProcessPath is { } path)
            key.SetValue(ValueName, $"\"{path}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
