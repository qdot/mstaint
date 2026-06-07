# Windows Pen Pressure Buttplug Control Plan

## Summary

Build a standalone Windows tray utility in C#/.NET 10 that reads full pen telemetry through the Win32 Pointer API and maps pen pressure to Buttplug device output via an external Intiface Central/Engine connection.

The first implementation should be a feasibility-first MVP: prove reliable pen capture while a painting program is in use, prove non-interruption of normal drawing, and prove safe Buttplug command output. The repo is currently empty, so the implementation should create a fresh .NET solution.

## Key Changes

- Create a C#/.NET 10 tray app with these subsystems:
  - `PenInput`: hidden/no-activate Win32 message window, `WM_POINTER*` handling, `GetPointerPenInfo`, full telemetry events for pressure, tilt, rotation, hover/contact, buttons, pointer id, timestamp, and screen position.
  - `ButtplugOutput`: Buttplug C# client connecting to external Intiface over WebSocket, device discovery, scalar/vibrate command routing, and stop-all handling.
  - `Mapping`: profile model with pressure min/max, deadzone, response curve, smoothing window, output clamp, and stale-input timeout.
  - `TrayUi`: arm/disarm toggle, Intiface connection status, selected device list, live telemetry/status, emergency stop, and settings entry point.

- Prioritise Win32 Pointer API first:
  - Start with a normal/local capture test window to validate `POINTER_PEN_INFO` data.
  - Then implement global capture with `RegisterPointerInputTarget(hwnd, PT_PEN)`.
  - Treat UIAccess as a gated milestone because Microsoft requires UIAccess privilege for global pointer redirection.

- Do not consume or modify drawing input:
  - The capture window must be hidden or no-activate.
  - Handlers should observe telemetry and avoid injecting pointer/mouse events.
  - The app sends Buttplug commands only when manually armed.

- Safety behaviour is mandatory:
  - Output is zero while disarmed.
  - Send stop on pen-up/contact lost, stale telemetry timeout, Intiface disconnect, device error, app exit, and emergency stop.
  - Use a conservative default stale timeout, e.g. 150 ms, configurable later.

- Default v1 mapping:
  - Pressure is normalised from Win32's `0..1024` pressure value when `PEN_MASK_PRESSURE` is present.
  - Apply deadzone, min/max calibration, exponential or gamma response curve, short smoothing, then send scalar vibration intensity `0.0..1.0`.
  - Capture full pen telemetry in the event model, but only pressure drives output in v1.

## Public Interfaces / Types

- `PenSample`
  - `PointerId`, `Timestamp`, `Position`, `InRange`, `InContact`, `PressureRaw`, `PressureNormalised`, `TiltX`, `TiltY`, `Rotation`, `PenFlags`, `PenMask`, `Buttons`.

- `PenInputService`
  - `StartLocalCapture(hwnd)`
  - `TryStartGlobalCapture()`
  - `Stop()`
  - `event SampleReceived`
  - `event CaptureError`

- `OutputProfile`
  - `IsArmed`
  - `PressureMin`, `PressureMax`, `Deadzone`, `Curve`, `SmoothingMs`, `StaleTimeoutMs`
  - `SelectedDeviceIds`

- `ButtplugOutputService`
  - `ConnectAsync(uri)`
  - `StartScanAsync()`
  - `SetIntensityAsync(deviceId, value)`
  - `StopAllAsync(reason)`

## Test Plan

- Unit-test pressure mapping:
  - zero pressure, deadzone, min/max calibration, curve response, smoothing, clamping, stale timeout.

- Unit-test safety:
  - disarm sends stop, pen-up sends stop, disconnect sends stop, app shutdown calls stop-all, stale input decays to stop.

- Windows manual test matrix:
  - Run local capture test window and verify pressure/tilt/buttons update.
  - Run global capture while drawing in Krita, Clip Studio Paint, Photoshop, and Paint where available.
  - Verify drawing still receives normal pressure and strokes are not interrupted.
  - Verify Intiface connection, device scan, selected-device output, emergency stop, and disconnect recovery.

- Packaging test:
  - Unsigned prototype should support local capture and non-UIAccess diagnostics.
  - Signed/UIAccess build should install under a trusted path such as `Program Files`, run with `uiAccess=true`, and successfully register global pen capture.

## Assumptions

- Use external Intiface Central/Engine for v1 rather than embedding the Buttplug server.
- Use C# because Buttplug C# is current client-only .NET code, and Win32 Pointer APIs are reachable through P/Invoke.
- Use .NET 10 LTS because Microsoft lists it as active LTS through November 2028.
- Wintab is not v1, but should be reserved as a later compatibility fallback for tablets/apps where Windows Ink/Pointer capture is insufficient.
- Main technical risk: global non-interrupting pen capture may require UIAccess signing and installation constraints. This must be proven before expanding UI polish.
- Reference docs used for this plan:
  - Buttplug C# docs: https://buttplug-csharp.docs.buttplug.io/
  - Buttplug overview: https://buttplug.io/
  - `RegisterPointerInputTarget`: https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerpointerinputtarget
  - `GetPointerPenInfo`: https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getpointerpeninfo
  - `WM_POINTERUPDATE`: https://learn.microsoft.com/windows/win32/inputmsg/wm-pointerupdate
  - Wacom Wintab overview: https://developer-docs.wacom.com/docs/icbt/windows/wintab/wintab-overview/
