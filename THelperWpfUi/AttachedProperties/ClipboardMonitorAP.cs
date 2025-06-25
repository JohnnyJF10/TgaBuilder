using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace THelperWpfUi.AttachedProperties
{
    public static class ClipboardMonitorAP
    {
        const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public static readonly DependencyProperty MonitorClipboardProperty =
            DependencyProperty.RegisterAttached(
                "MonitorClipboard",
                typeof(bool),
                typeof(ClipboardMonitorAP),
                new PropertyMetadata(false, OnMonitorClipboardChanged));

        public static readonly DependencyProperty ClipboardChangedCommandProperty =
            DependencyProperty.RegisterAttached(
                "ClipboardChangedCommand",
                typeof(ICommand),
                typeof(ClipboardMonitorAP),
                new PropertyMetadata(null));

        private static void OnMonitorClipboardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window) return;

            bool isEnabled = (bool)e.NewValue;
            if (isEnabled)
                EnableClipboardMonitoring(window);
            else
                DisableClipboardMonitoring(window);
        }

        private static void EnableClipboardMonitoring(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            AddClipboardFormatListener(hwnd);
            HwndSource hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource.AddHook(WndProc);
        }

        private static void DisableClipboardMonitoring(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            RemoveClipboardFormatListener(hwnd);
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source?.RemoveHook(WndProc);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            if (msg != WM_CLIPBOARDUPDATE) return IntPtr.Zero;
            if (GetWindowFromHandle(hwnd) is not Window window) return IntPtr.Zero;
            if (GetClipboardChangedCommand(window) is not ICommand command) return IntPtr.Zero;
            if (!command.CanExecute(null)) return IntPtr.Zero;

            command.Execute(null);
            handled = true;
            return IntPtr.Zero;
        }

        private static Window? GetWindowFromHandle(IntPtr hwnd)
        {
            if (HwndSource.FromHwnd(hwnd) is HwndSource hwndSource &&
                hwndSource.RootVisual is Window window)
                return window;

            return null;
        }

        public static void SetMonitorClipboard(DependencyObject element, bool value)
        {
            element.SetValue(MonitorClipboardProperty, value);
        }

        public static bool GetMonitorClipboard(DependencyObject element)
        {
            return (bool)element.GetValue(MonitorClipboardProperty);
        }

        public static void SetClipboardChangedCommand(DependencyObject element, ICommand value)
        {
            element.SetValue(ClipboardChangedCommandProperty, value);
        }

        public static ICommand GetClipboardChangedCommand(DependencyObject element)
        {
            return (ICommand)element.GetValue(ClipboardChangedCommandProperty);
        }
    }
}
