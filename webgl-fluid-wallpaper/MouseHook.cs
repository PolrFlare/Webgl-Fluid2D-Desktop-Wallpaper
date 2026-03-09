using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace webgl_fluid_wallpaper
{
    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc; // instance delegate to prevent GC
        private Action<double, double> _onMouseMove;
        private bool _paused = false;
        private bool _isDisposed = false;

        public MouseHook()
        {
            // Assign the callback delegate once for this instance
            _proc = HookCallback;
        }

        public void Start(Action<double, double> onMouseMoveCallback)
        {
            if (_hookID != IntPtr.Zero)
                Stop(); // unhook if already hooked

            _onMouseMove = onMouseMoveCallback;
            _hookID = SetHook(_proc);
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            _onMouseMove = null;
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _onMouseMove != null && !_paused)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                double normalizedX = (double)hookStruct.pt.x / SystemParameters.PrimaryScreenWidth;
                double normalizedY = 1.0 - ((double)hookStruct.pt.y / SystemParameters.PrimaryScreenHeight);

                try
                {
                    _onMouseMove.Invoke(normalizedX, normalizedY);
                }
                catch
                {
                    // Ignore or log exceptions from callback
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region Native stuff

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                Stop(); // unhook unmanaged resources
                _isDisposed = true;
            }
        }

        ~MouseHook()
        {
            Dispose(false);
        }

        #endregion
    }
}