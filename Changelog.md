# Changelog

## Version v0.0.1.2 (unreleased)
### Added
- Side navigation bar
- Redesigned UI
- Support for changing themes and navigation styles
- Icons for audio devices

### Known Issues
- Audio latency (~100-150ms delay) occurs when streaming from wireless to wired devices, but not when streaming from wired to wireless devices, so the latter configuration is recommended.
- No in-app option to change the system default output device (users must change it via Windows settings).

## Version v0.0.1.1 (unreleased)
### Added
- Synchronized system volume changes back to each device's volume slider

### Known Issues
- No in-app option to change the system default output device (users must change it via Windows settings).

## Version v0.0.1.0 (unreleased)
### Added
- Resolved crash when the default audio device is changed: the app now gracefully restarts capture and reattaches outputs without throwing exceptions.
- Enabled individual device volume sliders: each selected output device exposes a working volume control slider in the UI.
- Implemented a circular buffer for audio data to prevent buffer-full conditions and ensure smooth playback across multiple devices.

### Known Issues
- System volume changes are not yet reflected in the individual device sliders (will sync in upcoming release).
- No in-app option to change the system default output device (users must change it via Windows settings).
