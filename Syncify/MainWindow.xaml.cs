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
using Syncify.Services;
using NAudio.CoreAudioApi;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Windows.ApplicationModel;
using System.Threading;
using Microsoft.UI.Xaml.Media.Imaging;
using Application = Microsoft.UI.Xaml.Application;

namespace Syncify
{
    public sealed partial class MainWindow : Window
    {
        public DeviceService DeviceService { get; }
        public AudioService AudioService { get; }
        private readonly AudioNotificationClient _notifier;
        private readonly MMDeviceEnumerator _enumerator = new();
        private string _currentNavigationStyle = "Left"; // Default
        private NotifyIcon _notifyIcon;
        private bool _minimizeToTray = true; // Default behavior
        private bool _closeRequested = false;
        private WindowId _windowId;
        private AppWindow _appWindow;
        
        // Handle the window closing event
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        private const int GWL_STYLE = -16;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int SW_MINIMIZE = 6;
        
        public MainWindow()
        {
            this.InitializeComponent();

            // 1) Apply Mica
            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };

            // 2) Extend Mica under the title bar buttons
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            var winId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _windowId = winId;
            _appWindow = AppWindow.GetFromWindowId(winId);

            _appWindow.Resize(new SizeInt32(1024, 768));

            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            
            // Handle window closing event
            _appWindow.Closing += AppWindow_Closing;

            // 3) Create the AudioService and DeviceService
            DeviceService = new DeviceService(DispatcherQueue);
            AudioService = new AudioService();
            _notifier = new AudioNotificationClient(DeviceService, AudioService, DispatcherQueue);
            _enumerator.RegisterEndpointNotificationCallback(_notifier);

            // 4) Apply navigation style from settings
            // We need to wait until the control is loaded for this to work properly
            InitializeNavigationView();
            
            // 5) Set up system tray icon
            InitializeSystemTray();
            
            // 6) Load settings
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("MinimizeToTray", out object minimizeToTray))
            {
                if (minimizeToTray is bool value)
                {
                    _minimizeToTray = value;
                    Debug.WriteLine($"Loaded MinimizeToTray setting: {_minimizeToTray}");
                }
            }
            else
            {
                // Default is true if setting doesn't exist
                localSettings.Values["MinimizeToTray"] = _minimizeToTray;
                Debug.WriteLine($"Setting default MinimizeToTray: {_minimizeToTray}");
            }
        }
        
        public void SetMinimizeToTray(bool value)
        {
            _minimizeToTray = value;
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["MinimizeToTray"] = value;
            Debug.WriteLine($"MinimizeToTray set to: {value}");
        }
        
        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            Debug.WriteLine("Window closing event triggered");
            
            // If this is a real close request (not minimizing to tray)
            if (_closeRequested)
            {
                // Clean up resources
                _notifyIcon?.Dispose();
                return;
            }
            
            // If minimize to tray is enabled, cancel the close and minimize instead
            if (_minimizeToTray)
            {
                args.Cancel = true;
                MinimizeToTray();
            }
            // Otherwise, let the window close normally
        }
        
        private void MinimizeToTray()
        {
            Debug.WriteLine("Minimizing to tray");
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            
            // Hide the window
            _appWindow.Hide();
            
            // Show notification if this is the first time
            bool notifiedBefore = ApplicationData.Current.LocalSettings.Values.ContainsKey("TrayNotificationShown");
            if (!notifiedBefore)
            {
                _notifyIcon.ShowBalloonTip(3000, "Syncify", "Syncify is still running in the system tray", ToolTipIcon.Info);
                ApplicationData.Current.LocalSettings.Values["TrayNotificationShown"] = true;
            }
        }
        
        private void InitializeSystemTray()
        {
            try
            {
                // Create a new instance of NotifyIcon
                _notifyIcon = new NotifyIcon
                {
                    Visible = true,
                    Text = "Syncify"
                };
                
                // Load the app icon
                _notifyIcon.Icon = LoadIconFromAssets();
                
                // Create a context menu
                var contextMenu = new ContextMenuStrip();
                
                // Add menu items
                var showItem = new ToolStripMenuItem("Show Syncify");
                showItem.Click += (sender, args) => ShowWindow();
                contextMenu.Items.Add(showItem);
                
                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (sender, args) => ExitApplication();
                contextMenu.Items.Add(exitItem);
                
                // Set the context menu
                _notifyIcon.ContextMenuStrip = contextMenu;
                
                // Double-click on the icon should show the app
                _notifyIcon.DoubleClick += (sender, args) => ShowWindow();
                
                Debug.WriteLine("System tray icon initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing system tray: {ex.Message}");
            }
        }
        
        private Icon LoadIconFromAssets()
        {
            try
            {
                // Use the specific .ico file directly
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Square44x44Logo.altform-lightunplated_targetsize-32.ico");
                
                if (File.Exists(iconPath))
                {
                    // Load the .ico file directly
                    return new Icon(iconPath);
                }
                else
                {
                    Debug.WriteLine($"Icon file not found at path: {iconPath}");
                    
                    // Fallback to system icon if the file doesn't exist
                    return SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading icon: {ex.Message}");
                return SystemIcons.Application;
            }
        }
        
        public void ShowWindow()
        {
            Debug.WriteLine("Showing window from tray");
            
            // First make the AppWindow visible
            _appWindow.Show();
            
            // Get the window handle
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            
            // Try multiple ways to bring window to foreground
            User32.SetForegroundWindow(hwnd);
            
            // Windows 10/11 API to activate the window
            User32.ShowWindow(hwnd, User32.SW_RESTORE);
            User32.ShowWindow(hwnd, User32.SW_SHOW);
            
            // Set focus to the window
            this.Activate();
            
            Debug.WriteLine("Window show and activation complete");
        }
        
        // Import SetForegroundWindow to bring window to foreground
        private static class User32
        {
            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
            
            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            
            public const int SW_RESTORE = 9;
            public const int SW_SHOW = 5;
            public const int SW_SHOWNA = 8;
        }
        
        private void ExitApplication()
        {
            Debug.WriteLine("Exiting application from tray");
            _closeRequested = true;
            Application.Current.Exit();
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
