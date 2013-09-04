using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSharp.Interop.SWResample
{
    public unsafe struct AudioConvert {
        public int channels;
        public int  in_simd_align_mask;
        public int out_simd_align_mask;
        
        //public conv_func_type *conv_f;
        public IntPtr conv_f;

        //public simd_func_type *simd_f;
        public IntPtr simd_f;
        
        public int *ch_map;
    
        /// <summary>
        /// silence input sample
        /// </summary>
        public fixed Byte silence[8];
}
}
