using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinForms = System.Windows.Forms;

namespace GammaTool;

/// <summary>Главное окно: ползунок, кнопка вкл/выкл, шестерёнка в углу.</summary>
public partial class FlyoutWindow : Window
{
    const double ShadowMargin = 12;   // должно совпадать с Margin у карточки в XAML
    const double Gap = 8;             // отступ карточки от края рабочей области

    bool _updating;
    bool _prewarming;
    DateTime _hiddenAt = DateTime.MinValue;

    public event Action<int>? IntensityChanged;
    public event Action? ToggleClicked;
    public event Action? SettingsClicked;
    public event Action? Hidden;

    /// Нужен, чтобы клик по иконке в трее, закрывший окно через Deactivated, не открыл его снова.
    public bool RecentlyHidden => (DateTime.UtcNow - _hiddenAt).TotalMilliseconds < 300;

    public FlyoutWindow()
    {
        InitializeComponent();

        IntensitySlider.ValueChanged += (_, e) =>
        {
            int v = (int)e.NewValue;
            ValueText.Text = $"{v}%";
            if (!_updating) IntensityChanged?.Invoke(v);
        };

        // Колёсико мыши над ползунком: шаг 1 %
        IntensitySlider.PreviewMouseWheel += (_, e) =>
        {
            IntensitySlider.Value += e.Delta > 0 ? 1 : -1;
            e.Handled = true;
        };

        PowerButton.Click += (_, _) => ToggleClicked?.Invoke();
        GearButton.Click += (_, _) => SettingsClicked?.Invoke();

        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Hide(); };
        Deactivated += (_, _) => Hide();
        IsVisibleChanged += OnVisibleChanged;
    }

    /// Привести внешний вид в соответствие с состоянием (без повторной отправки событий).
    public void UpdateState(bool enabled, int intensity)
    {
        _updating = true;
        IntensitySlider.Value = Math.Clamp(intensity, GammaController.MinIntensity, GammaController.MaxIntensity);
        ValueText.Text = $"{(int)IntensitySlider.Value}%";
        PowerButton.IsChecked = enabled;                       // цвет кнопки меняется анимацией из Theme.xaml
        PowerButton.Content = enabled ? "Выключить" : "Включить";
        _updating = false;
    }

    /// Показать окно у правого нижнего угла рабочей области того монитора, где курсор.
    public void ShowAtTray()
    {
        var cursor = WinForms.Cursor.Position;                          // пиксели
        var area = WinForms.Screen.FromPoint(cursor).WorkingArea;       // пиксели
        double scale = ScaleAt(cursor.X, cursor.Y);                     // 1.0 = 100 %, 1.5 = 150 %...

        Show();

        // Позицию задаём в реальных пикселях, это надёжнее Left/Top при разном масштабе мониторов
        int x = (int)Math.Round(area.Right - (Width + Gap - ShadowMargin) * scale);
        int y = (int)Math.Round(area.Bottom - (Height + Gap - ShadowMargin) * scale);
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

        Activate();
    }

    /// Невидимый «прогрев»: создаёт окно и загружает XAML заранее, чтобы первый клик по иконке не тормозил.
    public void Prewarm()
    {
        _prewarming = true;
        ShowActivated = false;
        Left = Top = -10000;
        Show();
        Hide();
        ShowActivated = true;
        _prewarming = false;
    }

    void OnVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_prewarming) return;

        if ((bool)e.NewValue)
        {
            PlayShowAnimation();
        }
        else
        {
            _hiddenAt = DateTime.UtcNow;
            Hidden?.Invoke();
        }
    }

    /// Появление: плавное проявление и лёгкий подъём снизу на 12 пикселей.
    void PlayShowAnimation()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
        {
            FillBehavior = FillBehavior.Stop
        });
        Slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = ease,
            FillBehavior = FillBehavior.Stop
        });
    }

    static double ScaleAt(int x, int y)
    {
        var monitor = NativeMethods.MonitorFromPoint(
            new NativeMethods.POINT { X = x, Y = y }, NativeMethods.MONITOR_DEFAULTTONEAREST);
        return NativeMethods.GetDpiForMonitor(monitor, 0, out uint dpiX, out _) == 0 ? dpiX / 96.0 : 1.0;
    }
}
