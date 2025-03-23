using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace ScreenOCR
{
    public class NativeHotkey
    {
        private const int WM_HOTKEY = 0x0312;
        private readonly ILogger _logger;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public enum ModifierKeys : uint
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }

        private readonly Window _window;
        private readonly int _hotkeyId;
        private readonly IntPtr _handle;
        private HwndSource? _source;
        private readonly Action _hotkeyAction;

        public NativeHotkey(Window window, int hotkeyId, ModifierKeys modifiers, uint key, Action hotkeyAction, ILogger logger)
        {
            _window = window;
            _hotkeyId = hotkeyId;
            _hotkeyAction = hotkeyAction;
            _logger = logger;

            // Get the handle of the window
            _handle = new WindowInteropHelper(_window).EnsureHandle();
            
            // Register hotkey
            if (!RegisterHotKey(_handle, _hotkeyId, (uint)modifiers, key))
            {
                _logger.LogError($"Failed to register hotkey. Error code: {Marshal.GetLastWin32Error()}");
                throw new InvalidOperationException($"Failed to register hotkey. Error code: {Marshal.GetLastWin32Error()}");
            }

            // Add hook to window message pump
            _source = HwndSource.FromHwnd(_handle);
            _source?.AddHook(HwndHook);

            _logger.LogInformation($"Hotkey {modifiers}+{key} registered successfully with ID {hotkeyId}");
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                _logger.LogDebug($"Hotkey {_hotkeyId} triggered");
                _hotkeyAction.Invoke();
                handled = true;
            }
            
            return IntPtr.Zero;
        }

        public void Unregister()
        {
            _source?.RemoveHook(HwndHook);
            UnregisterHotKey(_handle, _hotkeyId);
            _logger.LogInformation($"Hotkey {_hotkeyId} unregistered");
        }
    }
}
