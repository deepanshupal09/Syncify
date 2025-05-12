using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AudioApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NAudio.CoreAudioApi;

namespace AudioApp
{
    public sealed partial class MainWindow : Window
    {
        public DeviceService DeviceService { get; }
        public AudioService AudioService { get; }
        private readonly AudioNotificationClient _notifier;

        public MainWindow()
        {
            InitializeComponent();

            DeviceService = new DeviceService(this.DispatcherQueue);
            AudioService = new AudioService();
            _notifier = new AudioNotificationClient(DeviceService, AudioService, this.DispatcherQueue);

            var enumerator = new MMDeviceEnumerator();
            enumerator.RegisterEndpointNotificationCallback(_notifier);

            RootGrid.DataContext = this;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            AudioService.StartCapture();

            // Wait for capture to initialize
            await Task.Delay(100);

            // Add outputs on UI thread
            foreach (var view in DeviceService.RenderDevices)
            {
                if (view.IsSelected && view.CanSelect)
                {
                    Debug.WriteLine($"Initializing output: {view.FriendlyName}");
                    if (AudioService.DeviceExists(view.ID))
                    {
                        // Retry logic for device initialization
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                AudioService.AddOutputDevice(view.ID);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Init attempt {i + 1} failed: {ex.Message}");
                                await Task.Delay(50);
                            }
                        }
                    }
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            AudioService.StopCapture();
        }

        private void Device_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string id } && AudioService.DeviceExists(id))
                AudioService.AddOutputDevice(id);
        }

        private void Device_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string id })
                AudioService.RemoveOutputDevice(id);
        }

        //private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        //{
        //    if (sender is Slider { Tag: string id })
        //        AudioService.SetDeviceVolume(id, (float)(e.NewValue / 100.0));
        //}
    }
}