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
3. Use the tray menu to connect Intiface, scan for devices, arm/disarm output, open the pen test window, or trigger emergency stop.

## Capture Notes

The app first tries `RegisterPointerInputTarget` for global pen capture. On normal unsigned builds this may fail because Windows global pointer redirection can require UIAccess signing and installation from a trusted location. If global capture fails, use the pen test window to validate local `WM_POINTER*` pressure/tilt/button telemetry.

The manifest currently sets `uiAccess="false"` intentionally. Change this only when the certificate, signing, and install-path requirements are ready to test on Windows.

