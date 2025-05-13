using System;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;          // <-- for NavigationView, Frame, NavigationViewItem
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;using Windows.Storage;  // <-- for LocalSettings
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AudioApp.Services;
using NAudio.CoreAudioApi;


namespace AudioApp
{
    public sealed partial class MainWindow : Window
    {
        public DeviceService DeviceService { get; }
        public AudioService AudioService { get; }
        private readonly AudioNotificationClient _notifier;
        private readonly MMDeviceEnumerator _enumerator = new();
        private string _currentNavigationStyle = "Left"; // Default
        
        public MainWindow()
        {
            this.InitializeComponent();

            // 1) Apply Mica
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };

            // 2) Extend Mica under the title bar buttons
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            var winId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(winId);

            appWindow.Resize(new SizeInt32(1024, 768));

            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // 3) Create the AudioService and DeviceService
            DeviceService = new DeviceService(DispatcherQueue);
            AudioService = new AudioService();
            _notifier = new AudioNotificationClient(DeviceService, AudioService, DispatcherQueue);
            _enumerator.RegisterEndpointNotificationCallback(_notifier);

            // 4) Apply navigation style from settings
            // We need to wait until the control is loaded for this to work properly
            InitializeNavigationView();
        }


        private void InitializeNavigationView()
        {
            // Apply saved navigation style or default to Left
            var localSettings = ApplicationData.Current.LocalSettings;
            
            if (localSettings.Values.TryGetValue("NavigationStyle", out object savedNavStyle) && savedNavStyle != null)
            {
                _currentNavigationStyle = savedNavStyle.ToString();
                Debug.WriteLine($"MainWindow initializing with navigation style: {_currentNavigationStyle}");
            }
            else
            {
                // Default to Left if not set
                _currentNavigationStyle = "Left";
                localSettings.Values["NavigationStyle"] = _currentNavigationStyle;
                Debug.WriteLine($"MainWindow setting default navigation style: {_currentNavigationStyle}");
            }
            
            // Force apply the navigation style, regardless of current state
            ApplyNavigationStyleForced(_currentNavigationStyle);
        }
        
        public void ApplyNavigationStyle(string navigationStyle)
        {
            // Only apply if it's different from current
            if (navigationStyle != _currentNavigationStyle)
            {
                Debug.WriteLine($"Changing navigation style from {_currentNavigationStyle} to {navigationStyle}");
                
                _currentNavigationStyle = navigationStyle;
                
                ApplyNavigationStyleInternal(navigationStyle);
            }
            else
            {
                Debug.WriteLine($"Navigation style already set to {navigationStyle}, no change needed");
            }
        }
        
        // Force the application of the style regardless of the current state
        private void ApplyNavigationStyleForced(string navigationStyle)
        {
            Debug.WriteLine($"Forcing navigation style to: {navigationStyle}");
            _currentNavigationStyle = navigationStyle;
            ApplyNavigationStyleInternal(navigationStyle);
        }
        
        // Internal implementation of the style application
        private void ApplyNavigationStyleInternal(string navigationStyle)
        {
            if (navigationStyle == "Top")
            {
                Debug.WriteLine("Setting navigation view to TOP mode");
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
            }
            else // Default to Left
            {
                Debug.WriteLine("Setting navigation view to LEFT mode");
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            }
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RootGrid.Loaded -= RootGrid_Loaded;
            AudioService.StartCapture();
            await Task.Delay(100);
            foreach (var dev in DeviceService.RenderDevices.Where(d => d.IsSelected && d.CanSelect))
            {
                for (int i = 0; i < 3; i++)
                {
                    try { AudioService.AddOutputDevice(dev.ID); break; }
                    catch { await Task.Delay(50); }
                }
            }
            NavView.SelectedItem = (NavigationViewItem)NavView.MenuItems[0];
        }


        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // ignore Settings (if you had any)
            if (args.IsSettingsSelected)
            {
                // Show SettingsPage
                ContentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag as string)
                {
                    case "general":
                        // Pass services as a parameter
                        ContentFrame.Navigate(
                            typeof(GeneralPage),
                            new { DeviceService = this.DeviceService, AudioService = this.AudioService }
                        );
                        break;
                    case "about":
                        ContentFrame.Navigate(typeof(AboutPage));
                        break;
                }
            }
        }
    }
}
