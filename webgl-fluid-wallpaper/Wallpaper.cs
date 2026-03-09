using Microsoft.Web.WebView2.Core;
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

        public Wallpaper()
        {
            Console.WriteLine("test");
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.ShowIcon = false;
            this.TopMost = true;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.BackColor = System.Drawing.Color.White;

            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            IntPtr progman = FindWindow("Progman", null);
            IntPtr result = IntPtr.Zero;

            SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, IntPtr.Zero,
                SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);

            IntPtr workerw = IntPtr.Zero;
            IntPtr desktopHandle = IntPtr.Zero;
            IntPtr shellViewWin = IntPtr.Zero;

            IntPtr temp = IntPtr.Zero;
            do
            {
                temp = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null);
                if (temp != IntPtr.Zero)
                {
                    IntPtr child = FindWindowEx(temp, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (child != IntPtr.Zero)
                    {
                        shellViewWin = child;
                        break;
                    }
                }
            } while (temp != IntPtr.Zero);

            specialWindowHandle = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null);
            if (specialWindowHandle != IntPtr.Zero)
            {
                SetParent(this.Handle, specialWindowHandle);
                SetWindowStyles(this.Handle);
            }
            _ = RenderWebViewAsync();
            //HideDisplay();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                Console.WriteLine("Initializing WebView2...");
                await RenderWebViewAsync();
                Console.WriteLine("WebView2 initialized successfully!");
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Console.WriteLine("WebView2 COMException: " + ex.Message);
                this.BackColor = Color.Blue; // fallback if WebView2 fails
            }
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

        public async Task RenderWebViewAsync()
        {
            await webView.EnsureCoreWebView2Async(); // null is optional

            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            Console.WriteLine(baseDir); // now this will print

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "wallpaper",
                baseDir,
                CoreWebView2HostResourceAccessKind.Allow
            );

            webView.CoreWebView2.Navigate("https://wallpaper/index.html");
        }
    }
}
