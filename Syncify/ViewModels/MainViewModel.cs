using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syncify.Services;
using System.Collections.ObjectModel;
using NAudio.CoreAudioApi;
using System.Linq;
using static Syncify.Services.DeviceService;
using System.Diagnostics;

namespace Syncify.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _currentDefaultDeviceId;

        // Line 13: Update property with proper attributes
        [ObservableProperty]
        private ObservableCollection<DeviceView> _renderDevices = new();
        public DeviceService DeviceService { get; }
        public AudioService AudioService { get; }

        public MainViewModel(DeviceService ds, AudioService asvc)
        {
            DeviceService = ds;
            AudioService = asvc;

            DeviceService.DefaultDeviceChanged += (s, e) => CurrentDefaultDeviceId = GetCurrentDefaultId();

            CurrentDefaultDeviceId = GetCurrentDefaultId();
        }

        [RelayCommand]
        private void Start() => AudioService.StartCapture();

        [RelayCommand]
        private void Stop() => AudioService.StopCapture();

        private string GetCurrentDefaultId()
        {
            using var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
        }

        [RelayCommand]
        private void SetAsDefault(string deviceId)
        {
            DeviceService.SetAsDefaultDevice(deviceId);
        }

        [RelayCommand]
        private void ToggleDevice(string deviceId)
        {
            var device = DeviceService.RenderDevices.First(d => d.ID == deviceId);

            Debug.WriteLine($"Toggled {deviceId} - Selected: {device.IsSelected}");

            if (device.IsSelected)
            {
                // Prevent deselecting last device
                if (AudioService.SelectedDevices.Count == 1) return;
                AudioService.RemoveOutputDevice(deviceId);
            }
            else
            {
                AudioService.AddOutputDevice(deviceId);
            }

            // Automatically set default if only one selected
            if (AudioService.SelectedDevices.Count == 1)
            {
                DeviceService.SetAsDefaultDevice(AudioService.SelectedDevices.First());
            }
        }

    }
}
