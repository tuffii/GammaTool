namespace GammaTool;

internal sealed record LanguageOption(string Code, string Title)
{
    public override string ToString() => Title;
}

internal static class L10n
{
    public const string En = "en";
    public const string Ru = "ru";

    public static readonly LanguageOption[] Languages =
    [
        new(En, "English"),
        new(Ru, "Русский"),
    ];

    static readonly Dictionary<string, Dictionary<string, string>> Text = new()
    {
        [En] = new()
        {
            ["Gamma"] = "Gamma",
            ["Enable"] = "Enable",
            ["Disable"] = "Disable",
            ["StateOn"] = "on",
            ["StateOff"] = "off",
            ["Settings"] = "Settings",
            ["Exit"] = "Exit",
            ["Monitor"] = "Monitor",
            ["Toggle"] = "Toggle",
            ["IntensityUp"] = "Intensity +",
            ["IntensityDown"] = "Intensity −",
            ["Step"] = "Step, %",
            ["Autostart"] = "Start with Windows",
            ["Cancel"] = "Cancel",
            ["SettingsTitle"] = "GammaTool — Settings",
            ["Language"] = "Language",
            ["NotSet"] = "(not set)",
            ["Primary"] = "primary",
            ["DuplicateHotkey"] = "The same shortcut is assigned to more than one action.",
            ["HotkeyBusy"] = "Could not register shortcuts (another program is using them): ",
            ["RampRejected"] = "Windows rejected the gamma ramp. Check the GdiIcmGammaRange value and reboot.",
            ["Tray"] = "Gamma: {0}% ({1})",
        },
        [Ru] = new()
        {
            ["Gamma"] = "Гамма",
            ["Enable"] = "Включить",
            ["Disable"] = "Выключить",
            ["StateOn"] = "вкл",
            ["StateOff"] = "выкл",
            ["Settings"] = "Настройки",
            ["Exit"] = "Выход",
            ["Monitor"] = "Монитор",
            ["Toggle"] = "Вкл / выкл",
            ["IntensityUp"] = "Интенсивность +",
            ["IntensityDown"] = "Интенсивность −",
            ["Step"] = "Шаг, %",
            ["Autostart"] = "Запускать вместе с Windows",
            ["Cancel"] = "Отмена",
            ["SettingsTitle"] = "GammaTool — настройки",
            ["Language"] = "Язык",
            ["NotSet"] = "(не назначено)",
            ["Primary"] = "основной",
            ["DuplicateHotkey"] = "Одно и то же сочетание назначено на несколько действий.",
            ["HotkeyBusy"] = "Не удалось занять сочетания (их использует другая программа): ",
            ["RampRejected"] = "Windows не принял гамма-рампу. Проверь ключ GdiIcmGammaRange и перезагрузись.",
            ["Tray"] = "Гамма: {0}% ({1})",
        },
    };

    static string _lang = En;

    public static event Action? LanguageChanged;

    public static string Language => _lang;

    public static void Set(string? lang)
    {
        string next = lang == Ru ? Ru : En;
        if (next == _lang) return;
        _lang = next;
        LanguageChanged?.Invoke();
    }

    public static string T(string key)
    {
        if (Text.TryGetValue(_lang, out var table) && table.TryGetValue(key, out var value))
            return value;
        return Text[En][key];
    }
}
