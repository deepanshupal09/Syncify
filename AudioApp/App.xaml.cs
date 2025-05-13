using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AudioApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? m_window;
        
        public static Window? MainWindow => (Current as App)?.m_window;

        /// <summary>
        /// Initializes the singleton application object.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            
            InitializeAppSettings();
            
            m_window.Activate();
        }

        private void InitializeAppSettings()
        {
            // Apply the saved theme and navigation style before activating the window
            if (m_window?.Content is FrameworkElement rootElement)
            {
                try
                {
                    // Get the saved theme
                    var localSettings = ApplicationData.Current.LocalSettings;
                    
                    // Theme handling
                    string themeName = "Default";
                    ElementTheme theme = ElementTheme.Default;
                    
                    // Check if theme is already saved
                    if (localSettings.Values.TryGetValue("Theme", out object savedTheme) && savedTheme != null)
                    {
                        themeName = savedTheme.ToString();
                        Debug.WriteLine($"Loading saved theme: {themeName}");
                    }
                    else
                    {
                        // No theme saved, initialize with default
                        localSettings.Values["Theme"] = themeName;
                        Debug.WriteLine($"Initializing theme setting to: {themeName}");
                    }
                    
                    // Convert theme name to ElementTheme
                    theme = themeName switch
                    {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };
                    
                    // Apply the theme
                    rootElement.RequestedTheme = theme;
                    
                    // Navigation style handling
                    string navStyle = "Left"; // Default
                    
                    // Check if navigation style is already saved
                    if (localSettings.Values.TryGetValue("NavigationStyle", out object savedNavStyle) && savedNavStyle != null)
                    {
                        navStyle = savedNavStyle.ToString();
                        Debug.WriteLine($"Loading saved navigation style: {navStyle}");
                    }
                    else
                    {
                        // No navigation style saved, initialize with default
                        localSettings.Values["NavigationStyle"] = navStyle;
                        Debug.WriteLine($"Initializing navigation style to: {navStyle}");
                    }
                    
                    // Apply navigation style
                    if (m_window is MainWindow mainWindow)
                    {
                        mainWindow.ApplyNavigationStyle(navStyle);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error initializing app settings: {ex.Message}");
                }
            }
        }
    }
}
