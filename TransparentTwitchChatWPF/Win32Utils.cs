using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TransparentTwitchChatWPF
{
    public static class WindowHelper
    {
        // Use SetWindowLongPtr for 64-bit compatibility
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Use GetWindowLongPtr for 64-bit compatibility
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x20;

        /// <summary>
        /// Makes the window click-through.
        /// </summary>
        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(extendedStyle.ToInt64() | WS_EX_TRANSPARENT));
        }

        /// <summary>
        /// Makes the window interactable.
        /// </summary>
        public static void SetWindowExDefault(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(extendedStyle.ToInt64() & ~WS_EX_TRANSPARENT));
        }

        public static void SetWindowClickThrough(IntPtr hwnd)
        {
            SetWindowExTransparent(hwnd);
        }

        public static void SetWindowInteractable(IntPtr hwnd)
        {
            SetWindowExDefault(hwnd);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;

        public static void SetWindowPosTopMost(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }
}
