using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace GammaTool;

/// <summary>Иконка в трее и связка всех частей между собой.</summary>
internal sealed class TrayApp
{
    readonly AppSettings _s = AppSettings.Load();
    readonly GammaController _gamma = new();
    readonly HotkeyWindow _hotkeys = new();
    readonly FlyoutWindow _flyout = new();
    readonly WinForms.NotifyIcon _tray = new();
    readonly WinForms.ToolStripMenuItem _menuToggle = new();
    readonly Dispatcher _ui = Dispatcher.CurrentDispatcher;         // для вызовов из чужих потоков
    readonly DispatcherTimer _reapplyTimer = new() { Interval = TimeSpan.FromMilliseconds(800) };
    readonly Drawing.Icon _iconOn = MakeIcon(Drawing.Color.FromArgb(0xE8, 0x59, 0x0C));   // акцент из Theme.xaml
    readonly Drawing.Icon _iconOff = MakeIcon(Drawing.Color.Gray);

    bool _errorShown;
    bool _settingsOpen;

    public TrayApp()
    {
        _gamma.SetDevice(_s.MonitorDevice);
        _gamma.SetIntensity(_s.Intensity);

        // хоткеи
        _hotkeys.HotkeyPressed += OnHotkey;
        RegisterHotkeys();

        // флайаут
        _flyout.IntensityChanged += v => SetIntensity(v);
        _flyout.ToggleClicked += () => SetEnabled(!_gamma.Enabled);
        _flyout.SettingsClicked += OpenSettings;
        _flyout.Hidden += () => _s.Save();

        // трей
        _menuToggle.Click += (_, _) => SetEnabled(!_gamma.Enabled);
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add(_menuToggle);
        menu.Items.Add("Настройки…", null, (_, _) => OpenSettings());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (_, e) => { if (e.Button == WinForms.MouseButtons.Left) ToggleFlyout(); };
        _tray.Visible = true;

        // переприменение по событиям системы (вместо опроса по таймеру)
        _reapplyTimer.Tick += (_, _) => { _reapplyTimer.Stop(); Report(_gamma.Reapply()); };
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        SystemEvents.PowerModeChanged += OnPowerChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        if (_s.Enabled) SetEnabled(true);
        RefreshUi();

        // «прогрев» флайаута в фоне, чтобы первое открытие было мгновенным
        _ui.BeginInvoke(DispatcherPriority.Background, new Action(_flyout.Prewarm));
    }

    // ---------- действия ----------

    void SetEnabled(bool on)
    {
        Report(_gamma.SetEnabled(on));
        _s.Enabled = _gamma.Enabled;
        _s.Save();
        RefreshUi();
    }

    void SetIntensity(int value)
    {
        _s.Intensity = Math.Clamp(value, 0, 100);
        Report(_gamma.SetIntensity(_s.Intensity));
        RefreshUi();
    }

    void OnHotkey(int id)
    {
        switch (id)
        {
            case HotkeyWindow.IdToggle:
                SetEnabled(!_gamma.Enabled);
                break;
            case HotkeyWindow.IdUp:
                SetIntensity(_s.Intensity + _s.StepPercent);
                _s.Save();
                break;
            case HotkeyWindow.IdDown:
                SetIntensity(_s.Intensity - _s.StepPercent);
                _s.Save();
                break;
        }
    }

    void RegisterHotkeys()
    {
        var failed = new List<string>();
        if (!_hotkeys.Register(HotkeyWindow.IdToggle, _s.Toggle, repeat: false)) failed.Add($"вкл/выкл ({_s.Toggle})");
        if (!_hotkeys.Register(HotkeyWindow.IdUp, _s.Up, repeat: true)) failed.Add($"плюс ({_s.Up})");
        if (!_hotkeys.Register(HotkeyWindow.IdDown, _s.Down, repeat: true)) failed.Add($"минус ({_s.Down})");

        if (failed.Count > 0)
            _tray.ShowBalloonTip(6000, "GammaTool",
                "Не удалось занять сочетания (их использует другая программа): " + string.Join(", ", failed),
                WinForms.ToolTipIcon.Warning);
    }

    void ToggleFlyout()
    {
        if (_flyout.IsVisible) { _flyout.Hide(); return; }
        if (_flyout.RecentlyHidden) return;
        _flyout.ShowAtTray();
    }

    void OpenSettings()
    {
        if (_settingsOpen) return;
        _settingsOpen = true;
        _flyout.Hide();
        _hotkeys.UnregisterAll();            // иначе наши же хоткеи перехватят нажатие в поле ввода

        try
        {
            var window = new SettingsWindow(_s);
            if (window.ShowDialog() != true) return;

            window.ReadInto(_s);
            _gamma.SetDevice(_s.MonitorDevice);
            Autostart.Set(_s.Autostart);
            _s.Save();
        }
        finally
        {
            RegisterHotkeys();
            RefreshUi();
            _settingsOpen = false;
        }
    }

    void ExitApp()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        SystemEvents.PowerModeChanged -= OnPowerChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _reapplyTimer.Stop();

        _s.Save();                  // _s.Enabled хранит последнее состояние для следующего запуска
        _gamma.Dispose();           // возвращает исходную гамму
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _flyout.Close();
        Application.Current.Shutdown();
    }

    // ---------- события системы (приходят из другого потока) ----------

    void OnDisplayChanged(object? sender, EventArgs e) => ScheduleReapply();

    void OnPowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) ScheduleReapply();
    }

    void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock) ScheduleReapply();
    }

    void ScheduleReapply() =>
        _ui.BeginInvoke(new Action(() => { _reapplyTimer.Stop(); _reapplyTimer.Start(); }));

    // ---------- интерфейс ----------

    void RefreshUi()
    {
        _tray.Icon = _gamma.Enabled ? _iconOn : _iconOff;
        _tray.Text = $"Гамма: {_s.Intensity}% ({(_gamma.Enabled ? "вкл" : "выкл")})";
        _menuToggle.Text = _gamma.Enabled ? "Выключить" : "Включить";
        _flyout.UpdateState(_gamma.Enabled, _s.Intensity);
    }

    void Report(bool ok)
    {
        if (ok) { _errorShown = false; return; }
        if (_errorShown) return;
        _errorShown = true;

        _tray.ShowBalloonTip(6000, "GammaTool",
            "Windows не принял гамма-рампу. Проверь ключ GdiIcmGammaRange и перезагрузись (раздел 2.2 инструкции).",
            WinForms.ToolTipIcon.Warning);
    }

    static Drawing.Icon MakeIcon(Drawing.Color color)
    {
        using var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);
            using var brush = new Drawing.SolidBrush(color);
            g.FillEllipse(brush, 4, 4, 24, 24);
        }
        return Drawing.Icon.FromHandle(bmp.GetHicon());     // две иконки на всё время жизни приложения
    }
}
