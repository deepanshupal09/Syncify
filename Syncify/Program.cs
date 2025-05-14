using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Syncify
{
    class Program
    {
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            bool isRedirect = await DecideRedirection();
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
            return 0;
        }

        private static async Task<bool> DecideRedirection()
        {
            bool isRedirect = false;
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;
            AppInstance keyInstance = AppInstance.FindOrRegisterForKey("Syncify_SingleInstance");
            
            if (keyInstance.IsCurrent) 
            {
                keyInstance.Activated += OnActivated;
            }
            else 
            {
                isRedirect = true;
                await keyInstance.RedirectActivationToAsync(args);
            }
            return isRedirect;
        }

        private static void OnActivated(object sender, AppActivationArguments args)
        {
            // This method will be called when the app is activated by another instance
            // We can add code here to handle specific activation types if needed
            ExtendedActivationKind kind = args.Kind;
        }
    }
} 