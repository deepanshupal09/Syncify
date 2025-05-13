using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Media.Devices;


namespace Syncify.Services
{
    public class DeviceView : INotifyPropertyChanged, IDisposable
    {
        public MMDevice Underlying { get; }
        public string ID => Underlying.ID;
        public string FriendlyName => Underlying.FriendlyName;

        private ImageSource _icon;
        public ImageSource Icon
        {
            get => _icon;
            set => SetField(ref _icon, value);
        }

        private int? _batteryPercentage;
        public int? BatteryPercentage
        {
            get => _batteryPercentage;
            set => SetField(ref _batteryPercentage, value);
        }
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
            _isMuted = device.AudioEndpointVolume.Mute;

            _ = LoadDeviceInfoAsync(device.ID);
        }

        public string VolumeIcon
        {
            get
            {
                if (IsMuted || Volume <= 0)
                    return "\uE74F"; // volume0
                else if (Volume < 33)
                    return "\uE993"; // volume1
                else if (Volume < 66)
                    return "\uE994"; // volume2
                else
                    return "\uE995"; // volume3
            } set
            {

            }
        }

        private bool _isMuted;
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    Underlying.AudioEndpointVolume.Mute = value;
                    OnPropertyChanged(nameof(IsMuted));
                    OnPropertyChanged(nameof(VolumeIcon));
                }
            }
        }

        private async Task LoadDeviceInfoAsync(string mmDeviceId)
        {
            // Use DeviceClass.AudioRender to get all audio devices
            var selector = MediaDevice.GetAudioRenderSelector();
            var allRenderers = await DeviceInformation.FindAllAsync(selector);

            // Match by FriendlyName (safer than ID)
            var di = allRenderers.FirstOrDefault(d =>
                d.Name == Underlying.FriendlyName
            );

            if (di == null) return;

            // Now load icon for this DeviceInformation
            await LoadIconAsync(di);
        }



        private async Task LoadIconAsync(DeviceInformation di)
        {
            //Debug.WriteLine($"Attempting to load icon for: {di.Name}");
            try
            {
                var icon = await di.GetThumbnailAsync();
                if (icon == null)
                {
                    Debug.WriteLine("Icon is null");
                    return;
                }

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(icon);
                //Debug.WriteLine("Icon loaded successfully");
                _dispatcherQueue.TryEnqueue(() => Icon = bitmap);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Icon load error: {ex}");
            }
        }

    
        private void OnVolumeNotification(AudioVolumeNotificationData data)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(Volume));
                OnPropertyChanged(nameof(VolumeRounded));
                OnPropertyChanged(nameof(VolumeIcon)); // <-- Add this
                OnPropertyChanged(nameof(IsMuted));
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
                    OnPropertyChanged(nameof(VolumeIcon)); // <-- Add this
                }
            }
        }


        public int VolumeRounded
        {
            get => (int)(Underlying.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            set
            {
                int newVolume = value;
                OnPropertyChanged(nameof(VolumeIcon)); // <-- Add this
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