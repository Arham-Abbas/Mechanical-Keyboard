using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using WinRT.Interop;

namespace Mechanical_Keyboard.Helpers
{
    public static class WindowExtensions
    {
        public static AppWindow GetAppWindow(this Window window)
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        public static void Hide(this Window window)
        {
            window.GetAppWindow().Hide();
        }
    }
}
