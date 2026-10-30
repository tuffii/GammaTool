using System.Windows.Interop;

namespace GammaTool;

/// <summary>Скрытое окно, которому система шлёт WM_HOTKEY.</summary>
internal sealed class HotkeyWindow : IDisposable
{
    public const int IdToggle = 1, IdUp = 2, IdDown = 3;

    readonly HwndSource _source;

    public event Action<int>? HotkeyPressed;

    public HotkeyWindow()
    {
        // Стиль 0 = обычное окно без WS_VISIBLE, то есть невидимое
        _source = new HwndSource(new HwndSourceParameters("GammaToolHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        });
        _source.AddHook(WndProc);
    }

    /// repeat = true: при удержании клавиши события повторяются (удобно для +/-).
    public bool Register(int id, HotkeyBinding binding, bool repeat)
    {
        NativeMethods.UnregisterHotKey(_source.Handle, id);
        if (binding.Key == 0) return true;

        uint mods = binding.Modifiers | (repeat ? 0 : NativeMethods.MOD_NOREPEAT);
        return NativeMethods.RegisterHotKey(_source.Handle, id, mods, binding.Key);
    }

    public void UnregisterAll()
    {
        foreach (int id in new[] { IdToggle, IdUp, IdDown })
            NativeMethods.UnregisterHotKey(_source.Handle, id);
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            HotkeyPressed?.Invoke(wParam.ToInt32());
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
