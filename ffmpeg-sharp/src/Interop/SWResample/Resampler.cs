using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSharp.Interop.SWResample
{
    public unsafe struct Resampler
    {
        //resample_init_func            init;
        public IntPtr init;
        //resample_free_func            free;
        public IntPtr free;
        //multiple_resample_func        multiple_resample;
        public IntPtr multiple_resample;
        //resample_flush_func           flush;
        public IntPtr flush;
        //set_compensation_func         set_compensation;
        public IntPtr set_compensation;
        //get_delay_func                get_delay;
        public IntPtr get_delay;
    }
}
