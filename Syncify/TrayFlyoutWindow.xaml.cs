using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using WinRT.Interop;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using Syncify.Services;
using Windows.Graphics;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;

namespace Syncify
{
    public class TrayFlyoutWindow
    {
        private Window _window;
        private AppWindow _appWindow;
        private TrayDeviceFlyout _flyoutContent;
        private IntPtr _hwnd;

        // Mouse hook
        private IntPtr _mouseHook = IntPtr.Zero;
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private HookProc _mouseProc;

        // Win32 constants for style
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private const int WS_SYSMENU = 0x00080000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_THICKFRAME = 0x00040000;

        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int WS_SIZEBOX = 0x00040000;  // Same as WS_THICKFRAME
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;



        // SetWindowPos flags
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_SHOWWINDOW = 0x0040;

        // Mouse messages
        private const int WH_MOUSE_LL = 14;
        private const uint WM_LBUTTONDOWN = 0x0201;

        public TrayFlyoutWindow(DeviceService deviceService, AudioService audioService)
        {
            // 1) Create WinUI window and set content
            _window = new Window();
            
            // Close the fly-out whenever it loses focus (more reliable than a global mouse hook)
            _window.Activated += OnWindowActivated;
            
            // Apply Mica backdrop like the main application
            _window.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.Base };
            
            _flyoutContent = new TrayDeviceFlyout(deviceService);
            _flyoutContent.SetAudioService(audioService);
            _flyoutContent.ShowAppRequested += OnShowAppRequested;
            _window.Content = _flyoutContent;

            // 2) Set up global mouse hook (detect clicks outside)
            SetupMouseHook();
        }

        public void ShowAtPosition(System.Drawing.Point point)
        {
            _window.Activate();

            if (_appWindow == null)
            {
                _hwnd = WindowNative.GetWindowHandle(_window);
                var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);

                // Create a custom presenter to remove all window chrome
                var presenter = _appWindow.Presenter as OverlappedPresenter;
                if (presenter == null)
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                    presenter = _appWindow.Presenter as OverlappedPresenter;
                }

                // Remove all window chrome
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsAlwaysOnTop = true;

                // Remove taskbar entry
                int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
                SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

                // Remove border styles completely
                int style = GetWindowLong(_hwnd, GWL_STYLE);
                style &= ~(WS_BORDER | WS_THICKFRAME | WS_DLGFRAME);
                SetWindowLong(_hwnd, GWL_STYLE, style);

                // Apply changes
                SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOZORDER | SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);

                // Enable modern window styling
                _window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                _window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            }

            // Keep existing resize/positioning code
            const int width = 350;
            const int height = 400;
            _appWindow.Resize(new SizeInt32(width, height));

            int x = point.X - width;
            int y = point.Y - height;
            SetWindowPos(_hwnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW);

            _appWindow.Show();
        }

        public void Close()
        {
            // Unhook the mouse hook
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            // Hide the flyout
            if (_appWindow != null && _appWindow.IsVisible)
            {
                _appWindow.Hide();
            }
        }

        private void SetupMouseHook()
        {
            _mouseProc = new HookProc(MouseHookCallback);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, IntPtr.Zero, 0);
            if (_mouseHook == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to set up mouse hook");
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _appWindow != null && _appWindow.IsVisible)
            {
                if ((uint)wParam == WM_LBUTTONDOWN)
                {
                    // Read mouse coordinates
                    int x = Marshal.ReadInt32(lParam);
                    int y = Marshal.ReadInt32(lParam, 4);
                    var pt = new POINT { X = x, Y = y };

                    // Determine the window under the cursor
                    IntPtr targetHwnd = WindowFromPoint(pt);

                    bool clickedInside = targetHwnd == _hwnd || IsChild(_hwnd, targetHwnd);

                    if (!clickedInside)
                    {
                        Debug.WriteLine("Click outside window detected, closing flyout");
                        // Hide on UI thread for reliability
                        _window.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (_appWindow != null && _appWindow.IsVisible)
                                _appWindow.Hide();
                        });
                    }
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private bool IsPointInWindow(POINT pt)
        {
            if (_hwnd == IntPtr.Zero)
                return false;

            if (GetWindowRect(_hwnd, out RECT rect))
            {
                return pt.X >= rect.left && pt.X <= rect.right
                    && pt.Y >= rect.top && pt.Y <= rect.bottom;
            }
            return false;
        }

        // P/Invoke declarations
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        // Event to notify when Show App is requested
        public event System.EventHandler ShowAppRequested;

        private void OnShowAppRequested(object sender, System.EventArgs e)
        {
            Debug.WriteLine("Show App requested from flyout");
            ShowAppRequested?.Invoke(this, e);
        }

        /// <summary>
        /// Hides the fly-out whenever it loses focus (window de-activated).
        /// This is more reliable than depending solely on the low-level mouse hook, and
        /// also works for keyboard focus changes, Alt-Tab, etc.
        /// </summary>
        private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                // Ensure we run on the UI thread
                _window.DispatcherQueue.TryEnqueue(() => _appWindow?.Hide());
            }
        }
    }
}
