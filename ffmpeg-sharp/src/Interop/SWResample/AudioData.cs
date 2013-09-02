using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpegSharp.Interop.Codec;

namespace FFmpegSharp.Interop.SWResample
{
    public unsafe struct AudioData
    {
        /// <summary>
        /// samples buffer per channel
        /// </summary>
        public fixed byte ch [SWResample.SWR_CH_MAX];

        /// <summary>
        /// samples buffer
        /// </summary>
        public Byte* data;

        /// <summary>
        /// number of channels
        /// </summary>
        public int ch_count;

        /// <summary>
        /// bytes per sample
        /// </summary>
        public int bps;

        /// <summary>
        /// number of samples
        /// </summary>
        public int count;

        /// <summary>
        /// 1 if planar audio, 0 otherwise
        /// </summary>
        public int planar;

        /// <summary>
        /// sample format
        /// </summary>
        public AVSampleFormat fmt;
    }
}
