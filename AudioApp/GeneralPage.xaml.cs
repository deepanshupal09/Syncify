using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AudioApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NAudio.CoreAudioApi;

namespace AudioApp
{
    public sealed partial class GeneralPage : Page
    {
        //public DeviceService DeviceService { get; }
        //public AudioService AudioService { get; }
        //private readonly AudioNotificationClient _notifier;
        //private readonly MMDeviceEnumerator _enumerator = new();


        private DeviceService DeviceService { get; set; }
        private AudioService AudioService { get; set; }

        public GeneralPage()
        {
            this.InitializeComponent();

            //DeviceService = new DeviceService(DispatcherQueue);
            //AudioService = new AudioService();
            //_notifier = new AudioNotificationClient(DeviceService, AudioService, DispatcherQueue);
            //_enumerator.RegisterEndpointNotificationCallback(_notifier);            this.Loaded += GeneralPage_Loaded; // Hook up the Loaded event
            this.DataContext = DeviceService;
        }

        // Retrieve services when the page is navigated to
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            dynamic services = e.Parameter;
            DeviceService = services.DeviceService;
            AudioService = services.AudioService;

            // Set the DataContext to DeviceService for binding
            this.DataContext = DeviceService;
        }


        private void Device_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string id } && AudioService.DeviceExists(id))
            {
                Debug.WriteLine($"Device_Checked: {id}");
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
                Debug.WriteLine($"Device_Unchecked: {id}");
                AudioService.RemoveOutputDevice(id);
            }
        }
    }
}
