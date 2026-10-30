using System.IO;

namespace GammaTool;

/// <summary>
/// Применяет гамму к одному монитору поверх его исходной рампы
/// и гарантированно возвращает всё назад.
/// </summary>
internal sealed class GammaController : IDisposable
{
    // ===== Кривая интенсивности (подгоняй под вкус) =====
    // При интенсивности 100 %: гамма = 1 + MaxGammaBoost, контраст = 1 + MaxContrastBoost.
    // Гамма 2.0 поднимает середину 0.5 -> ~0.71.
    // Контраст > 1 обрезает самые тёмные и самые светлые уровни, поэтому по умолчанию 0.
    // Для «как в BrightRaider» поставь MaxContrastBoost = 0.10.
    const double MaxGammaBoost = 1.0;
    const double MaxContrastBoost = 0.0;

    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GammaTool");
    static readonly string FlagPath = Path.Combine(Dir, "active.flag");

    readonly Dictionary<string, ushort[]> _baseline = new();   // исходные рампы по именам устройств
    readonly HashSet<string> _touched = new();                 // устройства, которые мы меняли
    string? _requestedDevice;                                  // null = основной

    public bool Enabled { get; private set; }
    public int Intensity { get; private set; }

    public GammaController()
    {
        Directory.CreateDirectory(Dir);
        RecoverAfterCrash();
        CaptureBaselines();
    }

    // ---------- публичный API ----------

    public void SetDevice(string? device)
    {
        _requestedDevice = device;
        if (!Enabled) return;
        RestoreTouched();
        Apply();
    }

    public bool SetEnabled(bool on)
    {
        if (on == Enabled) return true;

        if (on)
        {
            Enabled = true;
            File.WriteAllText(FlagPath, "1");
            if (Apply()) return true;

            // Windows не принял рампу: откатываемся
            Enabled = false;
            RestoreTouched();
            DeleteFlag();
            return false;
        }

        Enabled = false;
        RestoreTouched();
        DeleteFlag();
        return true;
    }

    public bool SetIntensity(int value)
    {
        Intensity = Math.Clamp(value, 0, 100);
        return Apply();
    }

    /// <summary>Повторно применить текущее состояние (после смены режима, выхода из сна и т.п.).</summary>
    public bool Reapply() => Apply();

    public void Dispose() => SetEnabled(false);

    // ---------- внутренности ----------

    bool Apply()
    {
        if (!Enabled) return true;

        string? device = ResolveTarget();
        if (device is null) return false;
        if (!EnsureBaseline(device)) return false;

        var ramp = Transform(_baseline[device], Intensity / 100.0);
        bool ok = SetRamp(device, ramp);
        if (ok) _touched.Add(device);
        return ok;
    }

    string? ResolveTarget()
    {
        var list = Monitors.List();
        if (_requestedDevice != null && list.Any(m => m.DeviceName == _requestedDevice))
            return _requestedDevice;
        return list.FirstOrDefault(m => m.IsPrimary)?.DeviceName ?? list.FirstOrDefault()?.DeviceName;
    }

    static ushort[] Transform(ushort[] src, double t)
    {
        double gamma = 1.0 + t * MaxGammaBoost;
        double contrast = 1.0 + t * MaxContrastBoost;

        var dst = new ushort[768];
        for (int i = 0; i < 768; i++)
        {
            double x = src[i] / 65535.0;
            x = (x - 0.5) * contrast + 0.5;
            x = Math.Clamp(x, 0.0, 1.0);
            x = Math.Pow(x, 1.0 / gamma);
            dst[i] = (ushort)Math.Round(Math.Clamp(x, 0.0, 1.0) * 65535.0);
        }
        return dst;
    }

    // ----- базовые рампы и восстановление -----

    static string BaselinePath(string device)
    {
        string safe = new string(device.Where(char.IsLetterOrDigit).ToArray());
        return Path.Combine(Dir, $"baseline_{safe}.bin");
    }

    bool EnsureBaseline(string device)
    {
        if (_baseline.ContainsKey(device)) return true;
        if (!GetRamp(device, out var ramp)) return false;
        SaveBaseline(device, ramp);
        return true;
    }

    void SaveBaseline(string device, ushort[] ramp)
    {
        _baseline[device] = ramp;
        var bytes = new byte[768 * 2];
        Buffer.BlockCopy(ramp, 0, bytes, 0, bytes.Length);
        File.WriteAllBytes(BaselinePath(device), bytes);
    }

    /// Если прошлый запуск упал при включённой гамме, возвращаем сохранённые базовые рампы.
    void RecoverAfterCrash()
    {
        if (!File.Exists(FlagPath)) return;

        foreach (var m in Monitors.List())
        {
            string path = BaselinePath(m.DeviceName);
            if (!File.Exists(path)) continue;

            var bytes = File.ReadAllBytes(path);
            if (bytes.Length != 768 * 2) continue;

            var ramp = new ushort[768];
            Buffer.BlockCopy(bytes, 0, ramp, 0, bytes.Length);
            SetRamp(m.DeviceName, ramp);
        }
        DeleteFlag();
    }

    /// На чистом старте запоминаем текущее состояние всех мониторов как «исходное».
    void CaptureBaselines()
    {
        _baseline.Clear();
        foreach (var m in Monitors.List())
            if (GetRamp(m.DeviceName, out var ramp))
                SaveBaseline(m.DeviceName, ramp);
    }

    void RestoreTouched()
    {
        foreach (var device in _touched)
            if (_baseline.TryGetValue(device, out var ramp))
                SetRamp(device, ramp);
        _touched.Clear();
    }

    static void DeleteFlag()
    {
        try { File.Delete(FlagPath); } catch { }
    }

    // ----- GDI -----

    static bool SetRamp(string device, ushort[] ramp)
    {
        IntPtr dc = NativeMethods.CreateDC("DISPLAY", device, null, IntPtr.Zero);
        if (dc == IntPtr.Zero) return false;
        try { return NativeMethods.SetDeviceGammaRamp(dc, ramp); }
        finally { NativeMethods.DeleteDC(dc); }
    }

    static bool GetRamp(string device, out ushort[] ramp)
    {
        ramp = new ushort[768];
        IntPtr dc = NativeMethods.CreateDC("DISPLAY", device, null, IntPtr.Zero);
        if (dc == IntPtr.Zero) return false;
        try { return NativeMethods.GetDeviceGammaRamp(dc, ramp); }
        finally { NativeMethods.DeleteDC(dc); }
    }
}
