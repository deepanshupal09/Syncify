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
}