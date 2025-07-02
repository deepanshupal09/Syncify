using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncify.Services;
using System.Diagnostics;

namespace Syncify
{
    // This is a UserControl that contains the device list
    public sealed partial class TrayDeviceFlyout : UserControl
    {
        public DeviceService DeviceService { get; set; }
        public AudioService AudioService { get; set; }

        // Event to notify when Show App button is clicked
        public event System.EventHandler ShowAppRequested;

        public TrayDeviceFlyout(DeviceService deviceService)
        {
            InitializeComponent();
            DeviceService = deviceService;
            
            // Set the data source
            DevicesListView.ItemsSource = deviceService.RenderDevices;
        }

        public void SetAudioService(AudioService audioService)
        {
            AudioService = audioService;
        }

        private void ShowAppButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Show App button clicked in flyout");
            ShowAppRequested?.Invoke(this, System.EventArgs.Empty);
        }

        private void Device_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string id } && AudioService?.DeviceExists(id) == true)
            {
                Debug.WriteLine($"Device_Checked in flyout: {id}");
                AudioService.AddOutputDevice(id);
            }
        }

        private void VolumeIcon_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FontIcon icon && icon.DataContext is DeviceView device)
            {
                device.IsMuted = !device.IsMuted;
            }
        }

        private void Device_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string id })
            {
                Debug.WriteLine($"Device_Unchecked in flyout: {id}");
                AudioService?.RemoveOutputDevice(id);
            }
        }
    }
}