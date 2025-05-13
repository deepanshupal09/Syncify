using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using NAudio.CoreAudioApi;

namespace Syncify.Services
{
    public class DeviceService
    {
        private readonly List<string> _selectedIds = new();
        public event EventHandler? DefaultDeviceChanged;

        public void UpdateSelectedDevices(IEnumerable<string> ids)
        {
            _selectedIds.Clear();
            _selectedIds.AddRange(ids);
        }

        public void SetAsDefaultDevice(string deviceId)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDevice(deviceId);

                // Windows Core Audio API call
                var policyConfig = new PolicyConfigClient();
                policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);

                RefreshDevices();
                DefaultDeviceChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (COMException ex)
            {
                Debug.WriteLine($"Failed to set default device: {ex.Message}");
                // Show user-friendly error in UI
            }
        }  
        

        [ComImport, Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
        private interface IPolicyConfig
        {
            void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, Role role);
        }

        // Add COM interop helper
        [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private class PolicyConfigClient : IPolicyConfig
        {
            [PreserveSig]
            public extern void SetDefaultEndpoint(string deviceId, Role role);
        }

        public ObservableCollection<DeviceView> RenderDevices { get; } = new();
        private readonly DispatcherQueue _uiQueue;

        public DeviceService(DispatcherQueue uiQueue)
        {
            _uiQueue = uiQueue;
            RefreshDevices();
        }

        public void RefreshDevices()
        {
            _uiQueue.TryEnqueue(() =>
            {
                foreach (var view in RenderDevices)
                {
                    view.Dispose(); // Properly dispose old views
                }
                RenderDevices.Clear();

                using var enumerator = new MMDeviceEnumerator();
                var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                var defaultId = defaultDevice.ID;

                foreach (var dev in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    var isDefault = dev.ID == defaultId;
                    var view = new DeviceView(dev, _uiQueue) // Pass dispatcher
                    {
                        CanSelect = !isDefault,
                        IsSelected = _selectedIds.Contains(dev.ID) || isDefault
                    };
                    RenderDevices.Add(view);
                }
                defaultDevice.Dispose();
            });
        }
    }
}