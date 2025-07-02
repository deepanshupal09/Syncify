using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;

namespace Syncify
{
    public class ChangelogItem
    {
        public string Text { get; set; }
    }

    public class ChangelogEntry
    {
        public string Version { get; set; }
        public string Status { get; set; }
        public ObservableCollection<ChangelogItem> Added { get; set; } = new();
        public ObservableCollection<ChangelogItem> Changed { get; set; } = new();
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public ObservableCollection<ChangelogEntry> ChangelogEntries { get; set; } = new();
        public ObservableCollection<ChangelogItem> KnownIssues { get; set; } = new();

        public AboutPage()
        {
            this.InitializeComponent();
            LoadChangelog();
            LoadKnownIssues();
        }

        private void LoadChangelog()
        {
            ChangelogEntries = new ObservableCollection<ChangelogEntry>
            {
                new() {
                    Version = "v0.1.0.0",
                    Status = "unreleased",
                    Added = new() {
                        new() { Text = "System tray flyout for device control and volume adjustment" },
                        new() { Text = "App logo integration in headers" }
                    },
                    Changed = new() {
                        new() { Text = "Minor UI fixes and improvements" }
                    }
                },
                new() {
                    Version = "v0.0.1.4",
                    Status = "unreleased",
                    Changed = new() {
                        new() { Text = "Made self contained app and fixed minor issues" }
                    }
                },
                new() {
                    Version = "v0.0.1.3",
                    Status = "unreleased",
                    Added = new() {
                        new() { Text = "Close to system tray option in settings" },
                        new() { Text = "Close to tray functionality" },
                        new() { Text = "Single instance application mode" }
                    },
                    Changed = new() {
                        new() { Text = "Updated About page" }
                    }
                },
                new() {
                    Version = "v0.0.1.2",
                    Status = "unreleased",
                    Added = new() {
                        new() { Text = "Side navigation bar" },
                        new() { Text = "Support for themes and navigation styles" },
                        new() { Text = "Icons for audio devices" }
                    },
                    Changed = new() {
                        new() { Text = "Redesigned UI" }
                    }
                },
                new() {
                    Version = "v0.0.1.1",
                    Status = "unreleased",
                    Added = new() {
                        new() { Text = "Synchronized system volume changes to device sliders" }
                    }
                },
                new() {
                    Version = "v0.0.1.0",
                    Status = "unreleased",
                    Added = new() {
                        new() { Text = "Graceful handling when default audio device changes (app now restarts capture and reattaches outputs without exceptions)" },
                        new() { Text = "Individual device volume sliders for each selected output device" },
                        new() { Text = "Circular buffer for audio data to prevent buffer-full conditions and ensure smooth playback across multiple devices" }
                    }
                }
            };
        }

        private void LoadKnownIssues()
        {
            KnownIssues = new ObservableCollection<ChangelogItem>
            {
                new() { Text = "Audio latency (~100-150ms) occurs when streaming from wireless to wired devices (recommend wired-to-wireless configuration)." },
                new() { Text = "No in-app control for setting the system default output device (must be changed via Windows settings)." }
            };
        }
    }
}
