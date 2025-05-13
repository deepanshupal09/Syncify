# README

## Syncify

AudioApp is a lightweight Windows 11 utility that lets you play system audio simultaneously on multiple output devices. By creating a virtual-loopback capture and mirroring the stream to all user-selected endpoints, AudioApp fills the gap in Windows’ native single-device audio routing.

### Key Features
- **Multi-Device Playback**: Route your system audio to any combination of active outputs (speakers, headphones, HDMI, USB audio).
- **Per-Device Volume Control**: Independently adjust the volume for each selected device.
- **Seamless Default Device Handling**: Automatically adapts when you switch the default audio device.
- **Robust Buffering**: A circular buffer implementation prevents overflows and dropouts when streaming to multiple sinks.
- **Clean WinUI3 Interface**: Intuitive, modern UI built with WinUI3 for native look and feel.
