using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace webgl_fluid_wallpaper
{
    public partial class Wallpaper : Form
    {
        private IntPtr specialWindowHandle = IntPtr.Zero;
        public Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private bool isWallpaperVisible = false;
        public event Action WallpaperReady; public Wallpaper()
        {
            InitializeComponent();
            var stdOut = Console.OpenStandardOutput();
            var writer = new StreamWriter(stdOut) { AutoFlush = true };
            Console.SetOut(writer);
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.ShowIcon = false;
            this.TopMost = false;
            // dual monitor support (were going to use the "span" method for now)
            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            this.Bounds = virtualScreen;
            this.BackColor = System.Drawing.Color.Black;

            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            IntPtr progman = FindWindow("Progman", null);
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                               SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out _);

            string windowsInfo = GetWindowsEdition();
            IntPtr workerw;
            if (windowsInfo.Contains("11"))
            {
                workerw = GetDesktopWorkerW_Win11();
            }
            else
            {
                workerw = GetDesktopWorkerW_Win10();
            }

            if (workerw != IntPtr.Zero)
            {
                SetParent(this.Handle, workerw);
                this.Bounds = SystemInformation.VirtualScreen;
                SetWindowStyles(this.Handle);
            }
            HideDisplay();
        }

        /// <summary>
        /// Windows 10 WorkerW logic: find WorkerW that comes after the one containing SHELLDLL_DefView
        /// </summary>
        private IntPtr GetDesktopWorkerW_Win10()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            IntPtr workerW = IntPtr.Zero;
            if (shellView == IntPtr.Zero)
            {
                do
                {
                    workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                    if (workerW != IntPtr.Zero)
                    {
                        IntPtr childShell = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                        shellView = childShell;
                    }
                } while (shellView == IntPtr.Zero && workerW != IntPtr.Zero);
            }
            IntPtr wallpaperWorkerW = IntPtr.Zero;
            EnumWindows((tophandle, topparamhandle) =>
            {
                IntPtr shell = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shell != IntPtr.Zero)
                {
                    IntPtr listView = FindWindowEx(shell, IntPtr.Zero, "SysListView32", null);
                    wallpaperWorkerW = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);
            return wallpaperWorkerW != IntPtr.Zero ? wallpaperWorkerW : progman;
        }

        /// <summary>
        /// Windows 11 WorkerW logic: there is usually a single WorkerW directly under Progman
        /// </summary>
        private IntPtr GetDesktopWorkerW_Win11()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            IntPtr workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            if (workerW != IntPtr.Zero)
            {
                return workerW;
            }
            workerW = IntPtr.Zero;
            do
            {
                workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                if (workerW != IntPtr.Zero)
                {
                    IntPtr childShell = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (childShell != IntPtr.Zero)
                    {
                        IntPtr next = IntPtr.Zero;
                        do
                        {
                            next = FindWindowEx(IntPtr.Zero, next, "WorkerW", null);
                        } while (next != IntPtr.Zero && FindWindowEx(next, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero);
                        return next != IntPtr.Zero ? next : workerW;
                    }
                }
            } while (workerW != IntPtr.Zero);
            return progman;
        }

        private string GetWindowsEdition()
        {
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                );
                if (key != null)
                {
                    string buildStr = key.GetValue("CurrentBuildNumber") as string ?? "0";
                    int build;
                    if (int.TryParse(buildStr, out build))
                    {
                        key.Close();
                        return build >= 22000 ? "11" : "10";
                    }
                    key.Close();
                }
            }
            catch
            {
            }
            return "10";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SetParent(this.Handle, IntPtr.Zero);
            base.OnFormClosing(e);
        }

        private void SetWindowStyles(IntPtr handle)
        {
            IntPtr exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            IntPtr newExStyle = (IntPtr)((long)exStyle | (long)WS_EX_TOOLWINDOW | (long)WS_EX_NOACTIVATE);
            SetWindowLong(handle, GWL_EXSTYLE, newExStyle);
        }

        // window style constants
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;


        // p/invoke declarations
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
            SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(Keys vKey);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr hWnd);

        const uint WM_SPAWN_WORKERW = 0x052C;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [Flags]
        enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0000,
            SMTO_BLOCK = 0x0001,
            SMTO_ABORTIFHUNG = 0x0002,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x0008
        }

        public void HideDisplay()
        {
            if (specialWindowHandle != IntPtr.Zero)
            {
                ShowWindow(specialWindowHandle, SW_HIDE);
            }
        }

        public void ShowDisplay()
        {
            if (specialWindowHandle != IntPtr.Zero)
            {
                ShowWindow(specialWindowHandle, SW_SHOW);
            }
        }

        public void ToggleDisplay()
        {
            if (isWallpaperVisible)
            {
                HideDisplay();
                isWallpaperVisible = false;
            }
            else
            {
                ShowDisplay();
                isWallpaperVisible = true;
            }
        }
        public async void RenderWebViewAsync()
        {
            webView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webView);

            await webView.EnsureCoreWebView2Async();

            webView.CoreWebView2.NavigationCompleted += (sender, args) =>
            {
                if (args.IsSuccess)
                {
                    ShowDisplay();
                    WallpaperReady?.Invoke();
                }
            };

            string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "wallpaper",
                assetsPath,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            );

            webView.CoreWebView2.Navigate("https://wallpaper/index.html");
        }

        public void UpdateWebView(string jsObject)
        {
            this.Invoke(new Action(() =>
            {
                if (webView?.CoreWebView2 != null)
                    webView.CoreWebView2.PostWebMessageAsJson(jsObject);

                if (jsObject.Contains("\"action\":\"display\""))
                {
                    //ShowDisplay();
                }
            }));
        }

        public void close()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(close));
                return;
            }

            HideDisplay();
            this.Close();
        }

        public void ResizeWallpaper(Rectangle virtualScreen)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ResizeWallpaper(virtualScreen)));
                return;
            }

            this.Left = virtualScreen.Left;
            this.Top = virtualScreen.Top;
            this.Width = virtualScreen.Width;
            this.Height = virtualScreen.Height;
        }
    }
}