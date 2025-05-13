// Services/AudioNotificationClient.cs
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using System;

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
                    // Clear any selection of the new default device
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
            // rarely needed; ignore or refresh if you care about volume changes
        }
    }
}
