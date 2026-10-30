using System.IO;
using System.Text.Json;

namespace GammaTool;

internal sealed class HotkeyBinding
{
    public uint Modifiers { get; set; }
    public uint Key { get; set; }          // код виртуальной клавиши (VK_*)

    public override string ToString()
    {
        if (Key == 0) return L10n.T("NotSet");

        var parts = new List<string>();
        if ((Modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((Modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((Modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((Modifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)Key).ToString());
        return string.Join("+", parts);
    }
}

internal sealed class AppSettings
{
    public int Intensity { get; set; } = 40;              // −50..150, 0 = исходная гамма
    public bool Enabled { get; set; }                     // состояние при следующем запуске
    public string? MonitorDevice { get; set; }            // null = основной монитор
    public int StepPercent { get; set; } = 5;             // шаг хоткеев +/-
    public bool Autostart { get; set; }
    public string Language { get; set; } = L10n.En;

    public HotkeyBinding Toggle { get; set; } = new()
    {
        Modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
        Key = NativeMethods.VK_G
    };

    public HotkeyBinding Up { get; set; } = new()
    {
        Modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
        Key = NativeMethods.VK_UP
    };

    public HotkeyBinding Down { get; set; } = new()
    {
        Modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
        Key = NativeMethods.VK_DOWN
    };

    // ---- хранение ----

    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GammaTool", "settings.json");

    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { /* битый файл: начинаем с настроек по умолчанию */ }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch { /* настройки не критичны */ }
    }
}
