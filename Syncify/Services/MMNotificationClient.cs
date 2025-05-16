using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Microsoft.UI.Dispatching;

namespace Syncify.Services
{
    public class AudioNotificationClient : IMMNotificationClient
    {
        private readonly DeviceService _deviceService;
        private readonly AudioService _audioService;
        private readonly DispatcherQueue _dispatcher;

        public AudioNotificationClient(
            DeviceService deviceService,
            AudioService audioService,
            DispatcherQueue dispatcher)
        {
            _deviceService = deviceService;
            _audioService = audioService;
            _dispatcher = dispatcher;
        }

        // Explicitly implement all interface methods
        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            => _deviceService.RefreshDevices();

        public void OnDeviceAdded(string pwstrDeviceId)
            => _deviceService.RefreshDevices();

        public void OnDeviceRemoved(string deviceId)
            => _deviceService.RefreshDevices();

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    foreach (var device in _deviceService.RenderDevices)
                    {
                        if (device.ID == defaultDeviceId && device.IsSelected)
                        {
                            device.IsSelected = false;
                        }
                    }
                    _deviceService.RefreshDevices();
                    _audioService.RestartOnDefaultChange();
                });
            }
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Optional: Handle property changes
        }
    }
}