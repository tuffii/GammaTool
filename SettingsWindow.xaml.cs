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

    void Set(HotkeyBinding b)
    {
        Binding = new HotkeyBinding { Modifiers = b.Modifiers, Key = b.Key };
        _box.Text = Binding.ToString();
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
            MessageBox.Show(this, "Одно и то же сочетание назначено на несколько действий.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
