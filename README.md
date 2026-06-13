# Windows Pen Control

Windows Pen Control is a C#/.NET tray utility prototype for controlling Buttplug devices from pen tablet telemetry.

The current slice targets Windows, connects to an external Intiface Central/Engine instance over WebSocket, and maps pen pressure to vibration intensity. The core mapping and safety logic is cross-platform and covered by tests; Win32 pen capture needs to be tested on Windows hardware.

## Build

```powershell
dotnet build WindowsPenControl.slnx
dotnet test tests/WindowsPenControl.Core.Tests/WindowsPenControl.Core.Tests.csproj
```

## Run On Windows

1. Start Intiface Central or Intiface Engine with the WebSocket server listening on `ws://127.0.0.1:12345`.
2. Run `src/WindowsPenControl`.
3. Open the settings window from the tray menu or by double-clicking the tray icon. The settings window and tray menu can both connect Intiface, scan for devices, arm/disarm output, open the pen test window, or trigger emergency stop.

## Capture Notes

The app starts a hidden capture window and enables passive capture paths:

- Raw Input with a background digitizer HID sink for installed tablet drivers that route paint apps through driver APIs such as WinTab.
- A minimal WinTab pressure context for driver stacks where Krita/other paint programs stop mirroring usable pressure through Windows pointer or HID reports.

The working split is not a single Windows Ink queue that Windows Pen Control receives and replays. The focused drawing app keeps its own Windows Ink/pointer stream, while Windows Pen Control listens to passive driver-exposed streams and maps those pressure samples to Intiface:

```mermaid
flowchart LR
    Tablet["Pen tablet + installed driver"] --> Ink["Windows Ink / pointer stream"]
    Tablet --> Raw["Raw Input HID reports"]
    Tablet --> WinTab["WinTab packets"]

    Ink --> Paint["Focused paint app<br/>Krita / Clip Studio Paint"]
    Raw --> WPC["Windows Pen Control<br/>passive capture"]
    WinTab --> WPC

    WPC --> Mapping["Pressure mapping"]
    Mapping --> Intiface["Intiface Central / Engine"]
    Intiface --> Devices["Haptics devices"]

    Redirect["RegisterPointerInputTarget<br/>disabled by default"] -. "would redirect Windows Ink to us" .-> WPC
```

The Raw Input path reads HID `Tip Pressure` reports and normalizes each device's logical pressure range into the existing `0..1024` pressure model. The WinTab path opens the default system context and requests only `X`, `Y`, `BUTTONS`, and `NORMAL_PRESSURE`. The capture service also gates samples by active input source so a zero-pressure or pen-up packet from one source cannot immediately stop output driven by pressure packets from another source.

`RegisterPointerInputTarget` is intentionally disabled by default because it redirects all pen pointer input to this app instead of teeing it; that breaks Windows Ink pressure in focused drawing apps such as Krita or Clip Studio Paint. For diagnostics only, set `WPC_ENABLE_POINTER_REDIRECT=1` before launching the app to enable Windows pointer redirection.

Keep the drawing app on its normal pressure API, such as Windows Ink/Windows 8+ Pointer Input for Krita. Windows Pen Control should receive pressure from the passive Raw HID or WinTab paths while the focused drawing app receives its own pressure stream.

Raw Input registration should work without UIAccess. If passive global paths fail, use the pen test window to validate local `WM_POINTER*` pressure/tilt/button telemetry.

The manifest currently sets `uiAccess="false"` intentionally. Change this only when the certificate, signing, and install-path requirements are ready to test on Windows.

