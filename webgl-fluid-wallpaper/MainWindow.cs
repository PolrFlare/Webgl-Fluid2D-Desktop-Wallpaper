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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace webgl_fluid_wallpaper
{
    public partial class MainWindow : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        Wallpaper wallpaper;
        private MouseHook mouseHook;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int useDark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }
        public MainWindow()
        {
            InitializeComponent();
            mouseHook = new MouseHook();
            Thread t = new Thread(() =>
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                wallpaper = new Wallpaper();

                wallpaper.Load += async (s, e) =>
                {
                    wallpaper.RenderWebViewAsync();
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

        private string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/config.json");
        private FluidConfig LoadConfig()
        {
            if (!System.IO.File.Exists(configPath))
                return new FluidConfig(); // fallback

            string json = System.IO.File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<FluidConfig>(json);
        }

        private void SaveConfig(FluidConfig config)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            System.IO.File.WriteAllText(configPath, json);
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
            wallpaper.UpdateWebView(json);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string value = comboBox1.SelectedItem.ToString();

            var message = new
            {
                action = "display",
                layer = "resolution",
                value = value
            };

            string json = JsonConvert.SerializeObject(message);
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
                    wallpaper.UpdateWebView(json);
                    var config = LoadConfig();
                    config.FluidColor = hex;
                    SaveConfig(config);
                }
            }
        }
    }
}
