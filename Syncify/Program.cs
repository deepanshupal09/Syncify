using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using System.Runtime.InteropServices;
using Syncify;
using System.Diagnostics;

namespace DrumPad
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            bool isRedirect = DecideRedirection();
            if (!isRedirect)
            {
                Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    new App();
                });
            }
        }

        private static bool DecideRedirection()
        {
            bool isRedirect = false;

            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;

            try
            {
                AppInstance keyInstance = AppInstance.FindOrRegisterForKey("randomKey");

                if (keyInstance.IsCurrent)
                {
                    keyInstance.Activated += OnActivated;
                }
                else
                {
                    isRedirect = true;
                    RedirectActivationTo(args, keyInstance);
                }
            }

            catch (Exception ex)
            {

            }

            return isRedirect;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
    IntPtr lpEventAttributes, bool bManualReset,
    bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("ole32.dll")]
        private static extern uint CoWaitForMultipleObjects(
            uint dwFlags, uint dwMilliseconds, ulong nHandles,
            IntPtr[] pHandles, out uint dwIndex);

        private static IntPtr redirectEventHandle = IntPtr.Zero;

        // Do the redirection on another thread, and use a non-blocking
        // wait method to wait for the redirection to complete.
        public static void RedirectActivationTo(
            AppActivationArguments args, AppInstance keyInstance)
        {
            redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                SetEvent(redirectEventHandle);
            });
            uint CWMO_DEFAULT = 0;
            uint INFINITE = 0xFFFFFFFF;
            _ = CoWaitForMultipleObjects(
               CWMO_DEFAULT, INFINITE, 1,
               new IntPtr[] { redirectEventHandle }, out uint handleIndex);
        }

        private static void OnActivated(object sender, AppActivationArguments args)
        {
            ExtendedActivationKind kind = args.Kind;
            Debug.WriteLine($"OnActivated: {kind}");
            
            // Use the UI dispatcher to show and activate the window
            try
            {
                Debug.WriteLine("Trying to access UI dispatcher");
                
                // Use the stored UI thread's dispatcher instead of the current thread's
                var uiDispatcher = Syncify.App.UIDispatcher;
                
                if (uiDispatcher != null)
                {
                    Debug.WriteLine("UI dispatcher found, enqueueing action");
                    
                    uiDispatcher.TryEnqueue(() =>
                    {
                        Debug.WriteLine("Running on UI thread");
                        if (App.MainWindow is MainWindow mainWindow)
                        {
                            Debug.WriteLine("Found main window, showing it");
                            mainWindow.ShowWindow();
                        }
                        else
                        {
                            Debug.WriteLine("Main window not found or not a MainWindow");
                        }
                    });
                }
                else
                {
                    Debug.WriteLine("UI dispatcher is null");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening instance {ex.Message}");
                // Handle any exceptions
            }
        }
    }
}