# Changelog

**Version v0.0.1.1** (unreleased)

## Added
- Synchronized system volume changes back to each device's volume slider by integrating AudioEndpointVolume notifications.

## Known Issues
- System volume changes are not yet reflected in the individual device sliders (will sync in upcoming release).
- No in-app option to change the system default output device (users must change it via Windows settings).

**Version v0.0.1.0** (unreleased)

## Added
- Resolved crash when the default audio device is changed: the app now gracefully restarts capture and reattaches outputs without throwing exceptions.
- Enabled individual device volume sliders: each selected output device exposes a working volume control slider in the UI.
- Implemented a circular buffer for audio data to prevent buffer-full conditions and ensure smooth playback across multiple devices.

## Known Issues
- System volume changes are not yet reflected in the individual device sliders (will sync in upcoming release).
- No in-app option to change the system default output device (users must change it via Windows settings).
