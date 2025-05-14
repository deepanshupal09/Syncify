using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Windows.Storage;  // <-- for LocalSettings
using System;
using System.Diagnostics;

namespace Syncify
{
    public sealed partial class SettingsPage : Page
    {
        private Window? _window;
        private bool _isInitializing = true;

        public SettingsPage()
        {
            this.InitializeComponent();
            _window = App.MainWindow;
            
            // Wait until the page is fully loaded before trying to set selections
            this.Loaded += SettingsPage_Loaded;
        }
        
        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSavedSelections();
        }
        
        private void LoadSavedSelections()
        {
            _isInitializing = true;
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                
                // THEME SELECTION
                string savedTheme = localSettings.Values["Theme"]?.ToString() ?? "Default";
                Debug.WriteLine($"Loading saved theme into settings: {savedTheme}");
                
                // Find the matching item without triggering the event
                bool foundTheme = false;
                foreach (ComboBoxItem item in ThemeSelector.Items)
                {
                    if (item.Tag?.ToString() == savedTheme)
                    {
                        // Set the selected item
                        ThemeSelector.SelectedItem = item;
                        foundTheme = true;
                        Debug.WriteLine($"Theme selected: {item.Tag}");
                        break;
                    }
                }
                
                // If nothing was selected, but we have a saved value
                if (!foundTheme && !string.IsNullOrEmpty(savedTheme))
                {
                    Debug.WriteLine($"Could not find theme item with tag '{savedTheme}'");
                    
                    // Try to find by index based on known values
                    switch (savedTheme)
                    {
                        case "Light": 
                            ThemeSelector.SelectedIndex = 1; 
                            break;
                        case "Dark": 
                            ThemeSelector.SelectedIndex = 2; 
                            break;
                        default: 
                            ThemeSelector.SelectedIndex = 0; 
                            break;
                    }
                }
                
                // NAVIGATION STYLE SELECTION
                string savedNavStyle = localSettings.Values["NavigationStyle"]?.ToString() ?? "Left";
                Debug.WriteLine($"Loading saved nav style into settings: {savedNavStyle}");
                
                // Find the matching item without triggering the event
                bool foundNavStyle = false;
                foreach (ComboBoxItem item in NavigationStyleSelector.Items)
                {
                    if (item.Tag?.ToString() == savedNavStyle)
                    {
                        // Set the selected item
                        NavigationStyleSelector.SelectedItem = item;
                        foundNavStyle = true;
                        Debug.WriteLine($"Nav style selected: {item.Tag}");
                        break;
                    }
                }
                
                // If nothing was selected, but we have a saved value
                if (!foundNavStyle && !string.IsNullOrEmpty(savedNavStyle))
                {
                    Debug.WriteLine($"Could not find nav style item with tag '{savedNavStyle}'");
                    
                    // Try to find by index based on known values
                    switch (savedNavStyle)
                    {
                        case "Top": 
                            NavigationStyleSelector.SelectedIndex = 1; 
                            break;
                        default: 
                            NavigationStyleSelector.SelectedIndex = 0; 
                            break;
                    }
                }
                
                // MINIMIZE TO TRAY CHECKBOX
                if (localSettings.Values.TryGetValue("MinimizeToTray", out object minimizeToTray) && 
                    minimizeToTray is bool value)
                {
                    MinimizeToTrayCheckBox.IsChecked = value;
                    Debug.WriteLine($"Minimize to tray checkbox set to: {value}");
                }
                else
                {
                    // Default is true
                    MinimizeToTrayCheckBox.IsChecked = true;
                    Debug.WriteLine("Minimize to tray checkbox defaulted to: true");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading saved selections: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip processing during initialization
            if (_isInitializing) return;
            
            if (ThemeSelector.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string selectedTheme = selectedItem.Tag.ToString();
                Debug.WriteLine($"Theme selection changed to: {selectedTheme}");
                
                // Save theme preference
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["Theme"] = selectedTheme;
                
                // Apply the theme to the root element
                ElementTheme newTheme = selectedTheme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };

                // For WinUI 3, we need to find the root element inside the window
                if (_window?.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = newTheme;
                }
                
                // Also apply to the current page for immediate feedback
                this.RequestedTheme = newTheme;
            }
        }
        
        private void NavigationStyleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip processing during initialization
            if (_isInitializing) return;
            
            if (NavigationStyleSelector.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string selectedNavStyle = selectedItem.Tag.ToString();
                Debug.WriteLine($"Navigation style changed to: {selectedNavStyle}");
                
                // Save navigation style preference
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["NavigationStyle"] = selectedNavStyle;
                
                // Apply to MainWindow
                if (_window is MainWindow mainWindow)
                {
                    mainWindow.ApplyNavigationStyle(selectedNavStyle);
                }
            }
        }
        
        private void MinimizeToTrayCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Skip processing during initialization
            if (_isInitializing) return;
            
            bool isChecked = MinimizeToTrayCheckBox.IsChecked ?? true;
            Debug.WriteLine($"Minimize to tray setting changed to: {isChecked}");
            
            // Save the setting
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["MinimizeToTray"] = isChecked;
            
            // Apply to MainWindow
            if (_window is MainWindow mainWindow)
            {
                mainWindow.SetMinimizeToTray(isChecked);
            }
        }
    }
}
