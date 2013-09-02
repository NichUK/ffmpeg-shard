using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSharp.Interop.SWResample
{
    public unsafe struct DitherContext 
    {
        public SwrDitherType method;
        public int noise_pos;
        public float scale;
        public float noise_scale;                              //< Noise scale
        public int ns_taps;                                    //< Noise shaping dither taps
        public float ns_scale;                                 //< Noise shaping dither scale
        public float ns_scale_1;                               //< Noise shaping dither scale^-1
        public int ns_pos;                                     //< Noise shaping dither position
        public float ns_coeffs[NS_TAPS];                       //< Noise shaping filter coefficients
        public float ns_errors[SWR_CH_MAX][2*NS_TAPS];
        public AudioData noise;                                //< noise used for dithering
        public AudioData temp;                                 //< temporary storage when writing into the input buffer isnt possible
        public int output_sample_bits;                         //< the number of used output bits, needed to scale dither correctly
    }
}
