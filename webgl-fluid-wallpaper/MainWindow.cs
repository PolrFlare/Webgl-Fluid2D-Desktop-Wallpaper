using Newtonsoft.Json;
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
        }
    }
}
