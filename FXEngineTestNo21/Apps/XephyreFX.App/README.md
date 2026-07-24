# XephyreFX

The animated weather blob app: a morphing black blob showing time/date/temperature/forecast,
with clouds, rain, thunderstorms, a night sky, a sun/moon that follows the actual time of day,
and a Valentine's Day mode (plus any custom date-triggered event you add yourself). Fully
themeable without touching code.

Cross-platform via Avalonia -- runs natively on Windows and Linux from the same source.

## Running it

```
cd Apps/XephyreFX.App
dotnet run
```

Requires the .NET 8 SDK. Two windows open: the scene (the blob itself) and Settings (all the
controls). Closing Settings closes the app.

## How it's organized

- **Two windows, on purpose.** `SceneWindow` is just the animated blob -- borderless when
  Desktop mode is on, so it can sit behind your desktop icons like a Rainmeter/Lively skin.
  `SettingsWindow` is a normal window with every control, and it's never affected by Desktop
  mode, so you can't lock yourself out of your own settings.
- **`Sim/`** -- the actual weather engine. Pure C#, no UI framework dependency at all.
- **`Rendering/`** -- the only Avalonia-aware drawing code.
- **`Config/`** -- `config.json` (auto-created next to the app) holds every customizable thing:
  blob colors, per-element text color/font/size/position, per-time-of-day sun/moon images,
  custom lines/images you add yourself. Hand-edit it or use the Settings window -- both work.
- **`Platform/`** -- OS-specific bits: run-at-startup, and the Windows-only desktop-embed trick.
- **`Events/`** -- drop a `.json` file here (see `valentines.json`) to add your own
  date-triggered overlay. No recompiling needed.

## Customizing the scene

Click almost anything in the scene window to select it -- the date, time, temperature,
condition, forecast, the sun/moon, the cloud spawn point, or any line/image you've added.
Drag to move it. With something selected, the Settings window shows its color/size/font (and
length/thickness for lines) so you can edit it there, then **Apply to selected**.

**Add divider line or image**, in Settings, drops a new one at the blob's center -- drag it
into place. **Browse...** buttons open a native file picker instead of needing to type a path.

## Desktop mode

Windows-only, and the most experimental thing in this project -- it uses the same
undocumented-but-widely-relied-on "WorkerW" trick every wallpaper-engine-style tool uses to
pin a window behind the desktop icons. If it doesn't work on your version of Windows, the
scene just stays a normal window; nothing breaks. Toggle it from the Settings window.

## Run at startup

Settings -> Startup -> Enable. Windows uses the registry Run key (via `reg.exe`, no extra
dependency); Linux drops a `.desktop` file in `~/.config/autostart`.

## Credits

Edit `credits` in `config.json` (or the placeholder in `YOUTUBE_LINK.txt`) to put your own
name/channel in the Settings window's Credits section.
