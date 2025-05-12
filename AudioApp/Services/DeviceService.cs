using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using NAudio.CoreAudioApi;

namespace AudioApp.Services
{
    public class DeviceService
    {
        private readonly List<string> _selectedIds = new();
        public event EventHandler? DefaultDeviceChanged;

        public void UpdateSelectedDevices(IEnumerable<string> ids)
        {
            _selectedIds.Clear();
            _selectedIds.AddRange(ids);
        }

        public void SetAsDefaultDevice(string deviceId)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDevice(deviceId);

                // Windows Core Audio API call
                var policyConfig = new PolicyConfigClient();
                policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);

                RefreshDevices();
                DefaultDeviceChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (COMException ex)
            {
                Debug.WriteLine($"Failed to set default device: {ex.Message}");
                // Show user-friendly error in UI
            }
        }

    
        

        [ComImport, Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
        private interface IPolicyConfig
        {
            void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, Role role);
        }

        // Add COM interop helper
        [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private class PolicyConfigClient : IPolicyConfig
        {
            [PreserveSig]
            public extern void SetDefaultEndpoint(string deviceId, Role role);
        }


        public class DeviceView : INotifyPropertyChanged, IDisposable
        {
            public MMDevice Underlying { get; }
            public string ID => Underlying.ID;
            public string FriendlyName => Underlying.FriendlyName;
            private readonly DispatcherQueue _dispatcherQueue;


            private bool _canSelect = true;
            public bool CanSelect
            {
                get => _canSelect;
                set => SetField(ref _canSelect, value);
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetField(ref _isSelected, value);
            }

            // Add this property
            private bool _isDefaultDevice;
            public bool IsDefaultDevice
            {
                get => _isDefaultDevice;
                set => SetField(ref _isDefaultDevice, value);
            }
            public DeviceView(MMDevice device, DispatcherQueue dispatcherQueue)
            {
                Underlying = device;
                _dispatcherQueue = dispatcherQueue;
                Underlying.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
            }

            private void OnVolumeNotification(AudioVolumeNotificationData data)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(Volume));
                });
            }

            public float Volume
            {
                get => Underlying.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                set
                {
                    float newVolume = Math.Clamp(value / 100f, 0f, 1f);
                    if (Math.Abs(Underlying.AudioEndpointVolume.MasterVolumeLevelScalar - newVolume) > 0.001f)
                    {
                        Underlying.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume;
                    }
                }
            }

            public void Dispose()
            {
                Underlying.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
                Underlying.Dispose();
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? nm = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nm));

            private void SetField<T>(ref T field, T value, [CallerMemberName] string? nm = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return;
                field = value;
                OnPropertyChanged(nm);
            }
        }

        public ObservableCollection<DeviceView> RenderDevices { get; } = new();
        private readonly DispatcherQueue _uiQueue;

        public DeviceService(DispatcherQueue uiQueue)
        {
            _uiQueue = uiQueue;
            RefreshDevices();
        }

        public void RefreshDevices()
        {
            _uiQueue.TryEnqueue(() =>
            {
                foreach (var view in RenderDevices)
                {
                    view.Dispose(); // Properly dispose old views
                }
                RenderDevices.Clear();

                using var enumerator = new MMDeviceEnumerator();
                var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                var defaultId = defaultDevice.ID;

                foreach (var dev in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    var isDefault = dev.ID == defaultId;
                    var view = new DeviceView(dev, _uiQueue) // Pass dispatcher
                    {
                        CanSelect = !isDefault,
                        IsSelected = _selectedIds.Contains(dev.ID) || isDefault
                    };
                    RenderDevices.Add(view);
                }
                defaultDevice.Dispose();
            });
        }
    }
}