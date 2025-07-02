# Changelog

## [0.1.0.0] - Unreleased

### Added
- System tray flyout for device control and volume adjustment
- App logo integration in headers

### Changed
- Minor UI fixes and improvements
  
## [0.0.1.4] - Unreleased

### Changed
- Made self contained app and fixed minor issues

## [0.0.1.3] - Unreleased

### Added
- "Close to system tray" option in app settings
- Close to tray functionality
- Single instance application mode (prevents multiple instances from running)

### Changed
- Updated About page

## [0.0.1.2] - Unreleased

### Added
- Side navigation bar
- Support for changing themes and navigation styles
- Icons for audio devices

### Changed
- Redesigned UI

### Known Issues
- Audio latency (~100-150ms delay) occurs when streaming from wireless to wired devices, but not when streaming from wired to wireless devices
- Recommended configuration: Use wired device as default audio device when streaming to wireless devices

## [0.0.1.1] - Unreleased

### Added
- Synchronized system volume changes back to each device's volume slider

### Known Issues
- No in-app option to change the system default output device (users must change it via Windows settings)

## [0.0.1.0] - Unreleased

### Added
- Graceful handling when default audio device changes (app now restarts capture and reattaches outputs without exceptions)
- Individual device volume sliders for each selected output device
- Circular buffer for audio data to prevent buffer-full conditions and ensure smooth playback across multiple devices

### Known Issues
- System volume changes not reflected in individual device sliders (fixed in v0.0.1.1)
- No in-app option to change the system default output device (users must change it via Windows settings)
