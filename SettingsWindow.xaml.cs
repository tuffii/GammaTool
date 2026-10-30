using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GammaTool;

/// <summary>Поле, в котором сочетание клавиш задаётся нажатием (вешается на обычный TextBox).</summary>
internal sealed class HotkeyField
{
    readonly TextBox _box;

    public HotkeyBinding Binding { get; private set; } = new();

    public HotkeyField(TextBox box, HotkeyBinding initial)
    {
        _box = box;
        _box.IsReadOnly = true;
        _box.IsUndoEnabled = false;
        _box.PreviewKeyDown += OnPreviewKeyDown;
        Set(initial);
    }

    public void Refresh() => _box.Text = Binding.ToString();

    void Set(HotkeyBinding b)
    {
        Binding = new HotkeyBinding { Modifiers = b.Modifiers, Key = b.Key };
        Refresh();
    }

    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // При нажатом Alt WPF присылает Key.System, а настоящая клавиша лежит в SystemKey
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Tab) return;                                                    // навигация Tab работает
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None) return;      // Esc закрывает окно

        e.Handled = true;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;        // один модификатор не принимаем
        if (key is Key.Back or Key.Delete) { Set(new HotkeyBinding()); return; }       // очистить

        var m = Keyboard.Modifiers;
        uint mods = 0;
        if (m.HasFlag(ModifierKeys.Control)) mods |= NativeMethods.MOD_CONTROL;
        if (m.HasFlag(ModifierKeys.Alt)) mods |= NativeMethods.MOD_ALT;
        if (m.HasFlag(ModifierKeys.Shift)) mods |= NativeMethods.MOD_SHIFT;

        // Без модификатора не принимаем: RegisterHotKey «съест» клавишу во всей системе
        if (mods == 0) return;

        Set(new HotkeyBinding { Modifiers = mods, Key = (uint)KeyInterop.VirtualKeyFromKey(key) });
    }
}

public partial class SettingsWindow : Window
{
    readonly HotkeyField _toggle, _up, _down;

    internal SettingsWindow(AppSettings s)
    {
        InitializeComponent();

        // --- данные ---
        var monitors = Monitors.List();
        MonitorBox.ItemsSource = monitors;                      // в списке показывается MonitorInfo.ToString()
        MonitorBox.SelectedItem = monitors.FirstOrDefault(m => m.DeviceName == s.MonitorDevice)
                                  ?? monitors.FirstOrDefault(m => m.IsPrimary);

        _toggle = new HotkeyField(ToggleBox, s.Toggle);
        _up = new HotkeyField(UpBox, s.Up);
        _down = new HotkeyField(DownBox, s.Down);

        StepSlider.ValueChanged += (_, e) => StepText.Text = ((int)e.NewValue).ToString();
        StepSlider.Value = Math.Clamp(s.StepPercent, 1, 25);
        StepText.Text = ((int)StepSlider.Value).ToString();

        AutostartBox.IsChecked = s.Autostart;

        LanguageBox.ItemsSource = L10n.Languages;
        LanguageBox.SelectedItem = L10n.Languages.First(o => o.Code == (s.Language == L10n.Ru ? L10n.Ru : L10n.En));
        LanguageBox.SelectionChanged += OnLanguageChanged;
        ApplyTexts();
    }

    void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageBox.SelectedItem is not LanguageOption opt) return;
        L10n.Set(opt.Code);
        ApplyTexts();
        ReloadMonitors();
        _toggle.Refresh();
        _up.Refresh();
        _down.Refresh();
    }

    void ApplyTexts()
    {
        Title = L10n.T("SettingsTitle");
        LanguageLabel.Text = L10n.T("Language");
        MonitorLabel.Text = L10n.T("Monitor");
        ToggleLabel.Text = L10n.T("Toggle");
        UpLabel.Text = L10n.T("IntensityUp");
        DownLabel.Text = L10n.T("IntensityDown");
        StepLabel.Text = L10n.T("Step");
        AutostartBox.Content = L10n.T("Autostart");
        OkButton.Content = "OK";
        CancelButton.Content = L10n.T("Cancel");
    }

    void ReloadMonitors()
    {
        string? selected = (MonitorBox.SelectedItem as MonitorInfo)?.DeviceName;
        var monitors = Monitors.List();
        MonitorBox.ItemsSource = monitors;
        MonitorBox.SelectedItem = monitors.FirstOrDefault(m => m.DeviceName == selected)
                                  ?? monitors.FirstOrDefault(m => m.IsPrimary);
    }

    /// Записать выбранное в объект настроек (вызывать после ShowDialog() == true).
    internal void ReadInto(AppSettings s)
    {
        s.MonitorDevice = (MonitorBox.SelectedItem as MonitorInfo)?.DeviceName;
        s.Toggle = _toggle.Binding;
        s.Up = _up.Binding;
        s.Down = _down.Binding;
        s.StepPercent = (int)StepSlider.Value;
        s.Autostart = AutostartBox.IsChecked == true;
        s.Language = (LanguageBox.SelectedItem as LanguageOption)?.Code ?? L10n.En;
    }

    void Ok_Click(object sender, RoutedEventArgs e)
    {
        // Одинаковые сочетания для двух действий не принимаем
        var used = new[] { _toggle.Binding, _up.Binding, _down.Binding }
            .Where(b => b.Key != 0)
            .Select(b => (b.Modifiers, b.Key))
            .ToList();

        if (used.Count != used.Distinct().Count())
        {
            MessageBox.Show(this, L10n.T("DuplicateHotkey"),
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
