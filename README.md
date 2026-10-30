# GammaTool

[English](README.md) · [Русский](README.ru.md)

Minimal Windows 11 tray utility for adjusting monitor gamma. Changes are written through the GDI gamma ramp.

![Main window](docs/images/flyout.jpg)

## Gamma range

| Slider | Gamma |
| --- | --- |
| −50% | 0.5 |
| 0% | 1.0 (unchanged) |
| 100% | 2.0 |
| 150% | 2.5 |

0% is your current picture. Negative values darken it, positive values brighten the midtones.

## Usage

- Left-click the tray icon to open the panel. Use the slider, or the mouse wheel over it.
- **Enable** / **Disable** turns the adjustment on or off.
- The gear icon opens settings.
- Right-click the tray icon for **Exit**.

![Settings](docs/images/settings.jpg)

**Settings**

- **Language**: English or Русский.
- **Monitor**: which display to adjust.
- **Toggle, Intensity +, Intensity −**: click a field and press a shortcut with Ctrl, Alt or Shift. Backspace clears it. Each shortcut must be unique.
- **Step, %**: how much each Intensity shortcut changes the slider. Holding the key repeats.
- **Start with Windows**: adds the current `.exe` path to your user autostart.

Default shortcuts: `Ctrl+Alt+G` toggle, `Ctrl+Alt+Up` and `Ctrl+Alt+Down` to change intensity.

## Install

1. Download `GammaTool.exe` from the [latest release](https://github.com/tuffii/GammaTool/releases/latest). It includes the .NET runtime, so nothing else needs to be installed.
2. Put it in a permanent folder, for example `C:\Tools\GammaTool\`. Autostart uses this path.
3. Run it. If SmartScreen warns about an unsigned app, choose **More info → Run anyway**.

### Optional: registry value

On some systems Windows rejects strong gamma ramps (for example, 2.0 and above) and the slider stops having effect. If that happens, set this value once from an **administrator** Command Prompt and reboot:

```bat
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM" /v GdiIcmGammaRange /t REG_DWORD /d 256 /f
```

To undo it:

```bat
reg delete "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM" /v GdiIcmGammaRange /f
```

The app itself does not need administrator rights and does not write to HKLM.

### Notes

- Test in SDR. With HDR on, the ramp is usually ignored.
- If the picture gets too bright, check whether a GPU control panel (for example AMD Custom Color) is also adjusting it.
- A virtual machine is fine for checking the UI, hotkeys and settings. Virtual display adapters such as QXL accept the ramp but do not change the picture, so check the effect on a real monitor.

## Behaviour

- Exit restores the original ramp. The same happens after a crash on the next start.
- Waking from sleep, unlocking the session or changing the display mode reapplies the current value.
- No network access and no telemetry.

## Build

Requires the [.NET SDK 10](https://dotnet.microsoft.com/download). The project cross-compiles from Linux.

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

The output is `bin/Release/net10.0-windows/win-x64/publish/GammaTool.exe`.

Pushing a `v*` tag runs `.github/workflows/release.yml`, which builds the exe on Windows and attaches it to a GitHub release.
