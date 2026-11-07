# DetroitIndicator

A desktop port of the **Detroit: Become Human** relationship gauge
(`GLOBAL_GAUGE_BENDING`), built with C# and WPF.

The indicator is a transparent, always-on-top overlay with a tray app,
an Apple-style settings window, auto-start, manual state previews, a localhost
TCP control channel, and basic network-availability switching.

![Seven gauge states](AffectionIndicator/docs/states.png)

## Features

- Transparent, click-through overlay with the seven source states.
- Extracted padlock textures and source-derived triangle backdrop, accordion
  plates, chevrons, big arrows, palette and 30 fps timing.
- Tray menu: show/hide, state previews, settings, topmost, network auto-switch,
  live network status, exit.
- Manual switching via preview buttons, tray, or TCP.
- Auto-start through `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- Adjustable gauge position (anchors, offsets, scale, drag) and independent
  horizontal/vertical **name position** controls in Appearance.
- Localhost TCP control on `127.0.0.1:8765`.

## Requirements

- Windows 10/11 x64
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- .NET 9 SDK to build

## Build and run

```powershell
dotnet build AffectionIndicator.sln -c Release
.\AffectionIndicator\bin\Release\net9.0-windows\win-x64\AffectionIndicator.exe
```

Start with `--autostart` to launch the tray app without opening Settings.

## Repository layout

```
AffectionIndicator/            WPF app, service layer, renderer, assets, README
indicator_demo/                dependency-free browser reference implementation
AFFECTION_INDICATOR_UI_REPORT.md  extraction and UI inventory notes
AFFECTION_UI_INVENTORY.csv        supporting inventory data
AffectionIndicator.sln
```

The app's detailed usage, TCP protocol, settings path, layout notes and
limitations are documented in
[`AffectionIndicator/README.md`](AffectionIndicator/README.md).

## Controls

- Double-click the tray icon to open Settings.
- Use **General → Manual preview** or the tray **Preview state** menu to show a
  state.
- Turn off **Click-through** on General, then drag the gauge to reposition it.
- Use **Appearance → Name position** to move the character name independently.

## Notes

This is a fan-made desktop experiment. Detroit: Become Human and its assets
belong to Quantic Dream; this project is not affiliated with or endorsed by
Quantic Dream. The extracted game data used during development is not included
in this repository. Only the small gauge textures needed by the app and the
reference viewer are included.

Settings are stored at
`%APPDATA%\AffectionIndicator\settings.json`.
