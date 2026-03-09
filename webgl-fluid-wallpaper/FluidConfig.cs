using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webgl_fluid_wallpaper
{
    public class FluidConfig
    {
        public string Resolution { get; set; }
        public float DensityDiffusion { get; set; }
        public float VelocityDiffusion { get; set; }
        public float Pressure { get; set; }
        public float Vorticity { get; set; }
        public float SplatRadius { get; set; }
        public bool SingleColor { get; set; }
        public bool MultiColor { get; set; }
        public string FluidColor { get; set; }
        public bool SunrayEnabled { get; set; }
        public bool ShadingEnabled { get; set; }
        public bool BloomEnabled { get; set; }
        public float BloomThreshold { get; set; }
        public float BloomIntensity { get; set; }
        public float SunrayWeight { get; set; }
        public string BackgroundColor { get; set; }
    }
}
