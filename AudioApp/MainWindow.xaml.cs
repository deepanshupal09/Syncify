using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AudioApp.Services;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Windows.Graphics;


namespace AudioApp
{
    public sealed partial class MainWindow : Window
    {
        public DeviceService DeviceService { get; }
        public AudioService AudioService { get; }
        private readonly AudioNotificationClient _notifier;
        private readonly MMDeviceEnumerator _enumerator = new();

        public MainWindow()
        {
            InitializeComponent();

            SystemBackdrop = new MicaBackdrop()
            { Kind = MicaKind.Base };

            // 2) grab your AppWindow…
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId winId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(winId);

            // 3) extend content under the title bar
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.Resize(new SizeInt32(1024, 768));

            // 4) make the caption buttons’ backgrounds transparent
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;

            DeviceService = new DeviceService(this.DispatcherQueue);
            AudioService = new AudioService();
            _notifier = new AudioNotificationClient(DeviceService, AudioService, this.DispatcherQueue);

            _enumerator.RegisterEndpointNotificationCallback(_notifier);

            // DataContext before anything else
            RootGrid.DataContext = this;
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Prevent double-init if Loaded fires more than once:
            Debug.WriteLine("RootGrid_Loaded fired");
            RootGrid.Loaded -= RootGrid_Loaded;

            // 1. Start capturing loopback
            AudioService.StartCapture();
            await Task.Delay(100); // let capture initialize

            // 2. Initialize all pre-selected devices
            foreach (var dev in DeviceService.RenderDevices.Where(d => d.IsSelected && d.CanSelect))
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        AudioService.AddOutputDevice(dev.ID);
                        break;
                    }
                    catch
                    {
                        await Task.Delay(50);
                    }
                }
            }
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1) Start capturing loopback
            AudioService.StartCapture();
            await Task.Delay(100); // let capture initialize

            // 2) Initialize all pre-selected devices
            foreach (var dev in DeviceService.RenderDevices.Where(d => d.IsSelected && d.CanSelect))
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        AudioService.AddOutputDevice(dev.ID);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Init attempt {attempt + 1} for {dev.FriendlyName} failed: {ex.Message}");
                        await Task.Delay(50);
                    }
                }
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Clean up audio
            AudioService.StopCapture();

            // Unregister notifications & dispose enumerator
            _enumerator.UnregisterEndpointNotificationCallback(_notifier);
            _enumerator.Dispose();
        }

        private void Device_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { Tag: string id } && AudioService.DeviceExists(id))
            {

            Debug.WriteLine($"Device_Checked: {id}");
                AudioService.AddOutputDevice(id);
            }
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
