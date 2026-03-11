using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace webgl_fluid_wallpaper
{
    public class AudioMonitor
    {
        private MMDevice device;
        private Timer timer;

        public event Action<int> OnAudioLevel;

        public void Start()
        {
            var enumerator = new MMDeviceEnumerator();
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            timer = new Timer(50);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (device == null) return;

            float peak = device.AudioMeterInformation.MasterPeakValue * 2.5f;
            peak = Math.Min(1f, peak);

            int intensity = 0;

            if (peak > 0.55f) intensity = 4;
            else if (peak > 0.5f) intensity = 3;
            else if (peak > 0.40f) intensity = 2;
            else if (peak > 0.15f) intensity = 1;

            if (intensity > 0)
                OnAudioLevel?.Invoke(intensity);
        }

        public void Stop()
        {
            timer?.Stop();
            timer?.Dispose();
        }
    }
}
