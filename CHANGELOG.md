# Changelog

## 1.0.1 - 2026-06-15

### Fixed

- Fall back to the Buttplug device name when Intiface does not provide a device display name.

## 1.0.0 - 2026-06-13

### Added

- Passive Raw Input HID and WinTab capture paths for relaying tablet pressure to Intiface while focused drawing apps keep their own Windows Ink pressure stream.
- Settings window with status, intensity, device list, Intiface controls, arm/disarm, pen test, emergency stop, and exit actions.
- Inno Setup installer for installing MSTaint into Program Files.
- README capture diagram showing the Windows Ink, Raw HID, WinTab, and Intiface data split.

### Changed

- Disabled `RegisterPointerInputTarget` by default because it redirects Windows Ink input away from drawing apps instead of passively teeing it.
- Updated capture status reporting so disabled pointer redirection is reported as a note, not an active capture source.
