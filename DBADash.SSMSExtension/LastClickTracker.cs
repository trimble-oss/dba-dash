using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Where a mouse button last went down on a results grid, and which grid - unless a key has been typed into a grid
    /// since - so a grid's context menu can be put down to the row that was right-clicked
    /// (<see cref="ResultsGridReader.TryGetFocusedRow"/>).  SSMS doesn't select the row or move the current cell on a
    /// right-click, and by the time a command runs the pointer is on the menu, so neither says which row it was.  Clicks
    /// anywhere else - the menu item itself, above all - are ignored.  A key typed into a grid forgets the click: a menu
    /// opened from the keyboard (the menu key, Shift+F10) is for the highlighted row.
    ///
    /// Thread hooks (WH_MOUSE, WH_KEYBOARD) on SSMS's own UI thread, set when the package loads: in-process, that thread
    /// only, and they only note a point and a window handle - every message is passed on unchanged.  Installed up front
    /// rather than on a grid's first menu, so the first click on a grid is seen too.
    /// </summary>
    internal static class LastClickTracker
    {
        private const int WH_KEYBOARD = 2;
        private const int WH_MOUSE = 7;
        private const int HC_ACTION = 0;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        // Kept referenced for as long as the hooks are set - the native side only holds a pointer to them.
        private static HookProc _mouseProc;
        private static HookProc _keyboardProc;
        private static IntPtr _mouseHook;
        private static IntPtr _keyboardHook;

        private static IntPtr _window;
        private static Point _screenPoint;
        private static bool _clicked;

        public static void Install()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_mouseHook != IntPtr.Zero) return;

            var thread = GetCurrentThreadId();
            _mouseProc = MouseProc;
            _keyboardProc = KeyboardProc;
            _mouseHook = SetWindowsHookEx(WH_MOUSE, _mouseProc, IntPtr.Zero, thread);
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD, _keyboardProc, IntPtr.Zero, thread);
        }

        public static void Uninstall()
        {
            if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
            if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
            _mouseHook = _keyboardHook = IntPtr.Zero;
            _clicked = false;
        }

        /// <summary>
        /// Where in <paramref name="control"/> the last click was, in its client coordinates - false if the last click was
        /// somewhere else, or a key has been pressed since.
        /// </summary>
        public static bool TryGetLastClick(Control control, out Point clientPoint)
        {
            clientPoint = Point.Empty;
            if (!_clicked || !control.IsHandleCreated || control.Handle != _window) return false;

            clientPoint = control.PointToClient(_screenPoint);
            return true;
        }

        private static IntPtr MouseProc(int code, IntPtr wParam, IntPtr lParam)
        {
            // Only clicks on a results grid: choosing the command from the context menu is a click too - on the menu - and
            // would otherwise replace the right-click the menu was opened with.
            if (code == HC_ACTION && (int)wParam is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN)
            {
                var info = Marshal.PtrToStructure<MOUSEHOOKSTRUCT>(lParam);
                if (ResultsGridReader.IsGridWindow(info.hwnd))
                {
                    _window = info.hwnd;
                    _screenPoint = new Point(info.pt.x, info.pt.y);
                    _clicked = true;
                }
            }

            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private static IntPtr KeyboardProc(int code, IntPtr wParam, IntPtr lParam)
        {
            // Bit 31 of lParam is the transition state: 0 for a key going down.  Only keys typed into a grid - the menu key,
            // Shift+F10, or moving the selection - not ones choosing an item from a menu the right-click opened.
            if (code == HC_ACTION && _clicked && ((long)lParam & 0x80000000) == 0 && ResultsGridReader.IsGridFocused()) _clicked = false;

            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEHOOKSTRUCT
        {
            public POINT pt;
            public IntPtr hwnd;
            public uint wHitTestCode;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
