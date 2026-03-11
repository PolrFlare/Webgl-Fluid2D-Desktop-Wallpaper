using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace webgl_fluid_wallpaper
{
    public partial class MainWindow : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool IsZoomed(IntPtr hWnd);

        const int GWL_STYLE = -16;
        const int WS_BORDER = 0x00800000;
        const int WS_CAPTION = 0x00C00000;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        Wallpaper wallpaper;
        private MouseHook mouseHook;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private System.Windows.Forms.Timer focusTimer;
        private Rectangle lastVirtualScreen;
        private AudioMonitor audioMonitor;

        private bool audioMonitorEnabled = false;

        private bool wallpaperEnabled = true;
        private bool startWithWindows = false;
        private bool pauseWhenFullscreen = false;
        private bool pauseWhenMaximized = false;
        private bool isExiting = false;

        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();

            var toggleDisplay = new ToolStripMenuItem("Disable Display");
            var startupItem = new ToolStripMenuItem("Start With Windows");
            var pauseItem = new ToolStripMenuItem("Pause When In Fullscreen");
            var pauseMaxItem = new ToolStripMenuItem("Pause When Window Maximized");
            var exitItem = new ToolStripMenuItem("Exit");

            startupItem.CheckOnClick = true;
            pauseItem.CheckOnClick = true;
            pauseMaxItem.CheckOnClick = true;


            startupItem.Checked = startWithWindows;
            pauseItem.Checked = pauseWhenFullscreen;
            pauseMaxItem.Checked = pauseWhenMaximized;
            if (!wallpaperEnabled)
                toggleDisplay.Text = "Enable Display";

            toggleDisplay.Click += (s, e) =>
            {
                wallpaperEnabled = !wallpaperEnabled;

                if (wallpaperEnabled)
                {
                    toggleDisplay.Text = "Disable Display";
                    wallpaper.ShowDisplay();
                }
                else
                {
                    toggleDisplay.Text = "Enable Display";
                    wallpaper.HideDisplay();
                }
                var config = LoadConfig();
                config.WallpaperEnabled = wallpaperEnabled;
                SaveConfig(config);
            };

            startupItem.CheckedChanged += (s, e) =>
            {
                startWithWindows = startupItem.Checked;
                SetStartup(startWithWindows);
                var config = LoadConfig();
                config.StartWithWindows = startWithWindows;
                SaveConfig(config);
            };

            pauseItem.CheckedChanged += (s, e) =>
            {
                pauseWhenFullscreen = pauseItem.Checked;
                var config = LoadConfig();
                config.PauseWhenFullscreen = pauseWhenFullscreen;
                SaveConfig(config);
            };

            pauseMaxItem.CheckedChanged += (s, e) =>
            {
                pauseWhenMaximized = pauseMaxItem.Checked;

                var config = LoadConfig();
                config.PauseWhenMaximized = pauseWhenMaximized;
                SaveConfig(config);
            };

            exitItem.Click += (s, e) =>
            {
                isExiting = true;
                trayIcon.Visible = false;
                mouseHook?.Stop();
                audioMonitor?.Stop();
                wallpaper?.close();
                Application.Exit();
            };

            trayMenu.Items.Add(toggleDisplay);
            trayMenu.Items.Add(startupItem);
            trayMenu.Items.Add(pauseItem);
            trayMenu.Items.Add(pauseMaxItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "WebGL Fluid Wallpaper";
            trayIcon.Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico"));
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };
        }

        private void StartFocusMonitor()
        {
            focusTimer = new System.Windows.Forms.Timer();
            focusTimer.Interval = 500;

            focusTimer.Tick += (s, e) =>
            {
                // check for any monitor res resizes
                Rectangle currentVirtual = SystemInformation.VirtualScreen;
                if (currentVirtual != lastVirtualScreen)
                {
                    lastVirtualScreen = currentVirtual;

                    if (wallpaper != null)
                        wallpaper.ResizeWallpaper(currentVirtual);
                }

                if ((!pauseWhenFullscreen && !pauseWhenMaximized) || wallpaper == null)
                    return;

                IntPtr foreground = GetForegroundWindow();
                IntPtr desktop = GetShellWindow();

                bool maximized = IsZoomed(foreground);

                if (foreground == IntPtr.Zero || foreground == desktop)
                {
                    SendFocus("desktop");
                    mouseHook.SetPaused(false);
                    return;
                }

                RECT rect;
                GetWindowRect(foreground, out rect);

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                Rectangle screen = Screen.FromHandle(foreground).Bounds;

                int style = GetWindowLong(foreground, GWL_STYLE);

                bool borderless = (style & WS_CAPTION) == 0;
                bool coversScreen =
                    rect.Left <= screen.Left &&
                    rect.Top <= screen.Top &&
                    rect.Right >= screen.Right &&
                    rect.Bottom >= screen.Bottom;

                bool fullscreen = borderless && coversScreen;

                bool pause = false;

                if (pauseWhenFullscreen && fullscreen)
                    pause = true;

                if (pauseWhenMaximized && maximized)
                    pause = true;

                if (pause)
                {
                    SendFocus("window");
                    mouseHook.SetPaused(true);
                }
                else
                {
                    SendFocus("desktop");
                    mouseHook.SetPaused(false);
                }
            };

            focusTimer.Start();
        }

        private void SendFocus(string focus)
        {
            var message = new
            {
                action = "focuschange",
                focus = focus
            };

            string json = JsonConvert.SerializeObject(message);

            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isExiting)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void SetStartup(bool enable)
        {
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = System.IO.Path.Combine(startupPath, "WebGLFluidWallpaper.lnk");

            if (enable)
            {
                if (!System.IO.File.Exists(shortcutPath))
                {
                    var shell = new IWshRuntimeLibrary.WshShell();
                    var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = Application.ExecutablePath;
                    shortcut.Save();
                }
            }
            else
            {
                if (System.IO.File.Exists(shortcutPath))
                    System.IO.File.Delete(shortcutPath);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int useDark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }

        public MainWindow()
        {
            InitializeComponent();
            lastVirtualScreen = SystemInformation.VirtualScreen;
            ApplyConfigToGUI();
            InitializeTray();
            StartFocusMonitor();
            mouseHook = new MouseHook();
            Thread t = new Thread(() =>
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                wallpaper = new Wallpaper();

                wallpaper.Load += async (s, e) =>
                {
                    wallpaper.RenderWebViewAsync(); ;
                };
                wallpaper.WallpaperReady += () =>
                {
                    if (!wallpaperEnabled)
                        wallpaper.HideDisplay();
                };

                mouseHookStart();
                System.Windows.Forms.Application.Run(wallpaper);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void mouseHookStart()
        {
            mouseHook.Start((x, y) =>
            {
                var message = new
                {
                    action = "mousemove",
                    x = x,
                    y = y
                };
                string json = JsonConvert.SerializeObject(message);
                wallpaper.UpdateWebView(json);
            });
        }

        private void StartAudioMonitor()
        {
            if (audioMonitor != null) return;
            audioMonitor = new AudioMonitor();
            audioMonitor.OnAudioLevel += HandleAudioLevel;
            audioMonitor.Start();
        }

        private void HandleAudioLevel(int level)
        {
            var message = new
            {
                action = "audio",
                value = level
            };
            string json = JsonConvert.SerializeObject(message);
            wallpaper?.UpdateWebView(json);
        }

        private void StopAudioMonitor()
        {
            if (audioMonitor == null) return;
            audioMonitor.Stop();
            audioMonitor = null;
        }

        private string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/config.json");
        private FluidConfig LoadConfig()
        {
            if (!System.IO.File.Exists(configPath))
                return new FluidConfig();

            string json = System.IO.File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<FluidConfig>(json);
        }

        private void SaveConfig(FluidConfig config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            System.IO.File.WriteAllText(configPath, json);
        }

        private void ApplyConfigToGUI()
        {
            var config = LoadConfig();

            startWithWindows = config.StartWithWindows;
            pauseWhenFullscreen = config.PauseWhenFullscreen;
            wallpaperEnabled = config.WallpaperEnabled;
            pauseWhenMaximized = config.PauseWhenMaximized;

            comboBox1.SelectedItem = config.Quality;
            comboBox2.SelectedItem = config.Resolution;

            trackBar1.Value = (int)(config.DensityDiffusion * 10);
            label4.Text = config.DensityDiffusion.ToString();

            trackBar2.Value = (int)(config.VelocityDiffusion * 100);
            label6.Text = config.VelocityDiffusion.ToString();

            trackBar3.Value = (int)(config.Pressure * 100);
            label8.Text = config.Pressure.ToString();

            trackBar4.Value = (int)(config.Vorticity);
            label10.Text = config.Vorticity.ToString();

            trackBar5.Value = (int)(config.SplatRadius * 100);
            label12.Text = config.SplatRadius.ToString();

            trackBar6.Value = (int)(config.BloomIntensity * 100);
            label14.Text = config.BloomIntensity.ToString();

            trackBar7.Value = (int)(config.BloomThreshold * 100);
            label16.Text = config.BloomThreshold.ToString();

            trackBar8.Value = (int)(config.SunrayWeight * 10);
            label18.Text = config.SunrayWeight.ToString();

            checkBox1.Checked = config.SingleColor;
            checkBox2.Checked = config.MultiColor;
            checkBox3.Checked = config.ShadingEnabled;
            checkBox4.Checked = config.BloomEnabled;
            checkBox5.Checked = config.SunrayEnabled;
            checkBox6.Checked = config.audioVisualizerEnabled;

            audioMonitorEnabled = config.audioVisualizerEnabled;

            if (audioMonitorEnabled)
            {
                StartAudioMonitor();
            }
            else
            {
                StopAudioMonitor();
            }

            button1.Text = config.BackgroundColor;
            button2.Text = config.FluidColor;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string value = comboBox1.SelectedItem.ToString();

            var message = new
            {
                action = "display",
                layer = "quality",
                value = value
            };

            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.Quality = value;
            SaveConfig(config);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string value = comboBox2.SelectedItem.ToString();

            var message = new
            {
                action = "display",
                layer = "resolution",
                value = value
            };

            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.Resolution = value;
            SaveConfig(config);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float value = trackBar1.Value / 10f;
            label4.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "densityDiffSlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.DensityDiffusion = value;
            SaveConfig(config);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            float value = trackBar2.Value / 100f;
            label6.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "velocityDiffSlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.VelocityDiffusion = value;
            SaveConfig(config);
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            float value = trackBar3.Value / 100f;
            label8.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "pressureSlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.Pressure = value;
            SaveConfig(config);
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            float value = trackBar4.Value / 1f;
            label10.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "vorticitySlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.Vorticity = value;
            SaveConfig(config);
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            float value = trackBar5.Value / 100f;
            label12.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "splatRadiusSlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.SplatRadius = value;
            SaveConfig(config);
        }

        private void trackBar6_Scroll(object sender, EventArgs e)
        {
            float value = trackBar6.Value / 100f;
            label14.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "bloomIntensitySlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.BloomIntensity = value;
            SaveConfig(config);
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            float value = trackBar7.Value / 100f;
            label16.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "bloomThresholdSlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.BloomThreshold = value;
            SaveConfig(config);
        }

        private void trackBar8_Scroll(object sender, EventArgs e)
        {
            float value = trackBar8.Value / 10f;
            label18.Text = value.ToString();

            var message = new
            {
                action = "display",
                layer = "sunrayWeightSlider",
                value = value
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.SunrayWeight = value;
            SaveConfig(config);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var message = new
            {
                action = "display",
                layer = "singlecolor",
                value = checkBox1.Checked
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.SingleColor = checkBox1.Checked;
            SaveConfig(config);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            var message = new
            {
                action = "display",
                layer = "multicolor",
                value = checkBox2.Checked
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.MultiColor = checkBox2.Checked;
            SaveConfig(config);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            var message = new
            {
                action = "display",
                layer = "shading",
                value = checkBox3.Checked
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.ShadingEnabled = checkBox3.Checked;
            SaveConfig(config);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            var message = new
            {
                action = "display",
                layer = "bloom",
                value = checkBox4.Checked
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.BloomEnabled = checkBox4.Checked;
            SaveConfig(config);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            var message = new
            {
                action = "display",
                layer = "sunrays",
                value = checkBox5.Checked
            };
            string json = JsonConvert.SerializeObject(message);
            if (wallpaper != null)
                wallpaper.UpdateWebView(json);
            var config = LoadConfig();
            config.SunrayEnabled = checkBox5.Checked;
            SaveConfig(config);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AllowFullOpen = true;
                colorDialog.AnyColor = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    System.Drawing.Color selectedColor = colorDialog.Color;
                    string hex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";

                    var message = new
                    {
                        action = "display",
                        layer = "bgcolor",
                        value = hex
                    };

                    string json = JsonConvert.SerializeObject(message);
                    if (wallpaper != null)
                        wallpaper.UpdateWebView(json);
                    var config = LoadConfig();
                    config.BackgroundColor = hex;
                    SaveConfig(config);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AllowFullOpen = true;
                colorDialog.AnyColor = true;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    System.Drawing.Color selectedColor = colorDialog.Color;
                    string hex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";

                    var message = new
                    {
                        action = "display",
                        layer = "fluidcolor",
                        value = hex
                    };

                    string json = JsonConvert.SerializeObject(message);
                    if (wallpaper != null)
                        wallpaper.UpdateWebView(json);
                    var config = LoadConfig();
                    config.FluidColor = hex;
                    SaveConfig(config);
                }
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            var config = LoadConfig();
            config.audioVisualizerEnabled = checkBox6.Checked;
            SaveConfig(config);

            audioMonitorEnabled = checkBox6.Checked;

            if (audioMonitorEnabled)
                StartAudioMonitor();
            else
                StopAudioMonitor();
        }
    }

}