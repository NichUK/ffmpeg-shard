using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FFmpegSharp.Interop.Codec;
using FFmpegSharp.Interop.Util;

namespace FFmpegSharp.Interop.SWResample
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SwrContext {
        /// <summary>
        /// AVClass used for AVOption and av_log()
        /// </summary>
        public AVClass *av_class;

        /// <summary>
        /// logging level offset
        /// </summary>
        public int log_level_offset;

        /// <summary>
        /// parent logging context
        /// </summary>
        public void* log_ctx;

        /// <summary>
        /// input sample format
        /// </summary>
        public AVSampleFormat in_sample_fmt;

        /// <summary>
        /// internal sample format (AV_SAMPLE_FMT_FLTP or AV_SAMPLE_FMT_S16P)
        /// </summary>
        public AVSampleFormat int_sample_fmt;

        /// <summary>
        /// output sample format
        /// </summary>
        public AVSampleFormat out_sample_fmt;

        /// <summary>
        /// input channel layout
        /// </summary>
        public Int64 in_ch_layout;

        /// <summary>
        /// output channel layout
        /// </summary>
        public Int64 out_ch_layout;

        /// <summary>
        /// input sample rate
        /// </summary>
        public int in_sample_rate;

        /// <summary>
        /// output sample rate
        /// </summary>
        public int out_sample_rate;

        /// <summary>
        /// miscellaneous flags such as SWR_FLAG_RESAMPLE
        /// </summary>
        public int flags;

        /// <summary>
        /// surround mixing level
        /// </summary>
        public float slev;

        /// <summary>
        /// center mixing level
        /// </summary>
        public float clev;

        /// <summary>
        /// LFE mixing level
        /// </summary>
        public float lfe_mix_level;

        /// <summary>
        /// rematrixing volume coefficient
        /// </summary>
        public float rematrix_volume;

        /// <summary>
        /// maximum value for rematrixing output
        /// </summary>
        public float rematrix_maxval;

        /// <summary>
        /// matrixed stereo encoding
        /// </summary>
        public AVMatrixEncoding matrix_encoding;

        /// <summary>
        /// channel index (or -1 if muted channel) map
        /// </summary>
        public int* channel_map;

        /// <summary>
        /// number of used input channels (mapped channel count if channel_map, otherwise in.ch_count)
        /// </summary>
        public int used_ch_count;

        public SwrEngine engine;

        public DitherContext dither;

        /// <summary>
        /// length of each FIR filter in the resampling filterbank relative to the cutoff frequency
        /// </summary>
        public int filter_size;

        /// <summary>
        /// log2 of the number of entries in the resampling polyphase filterbank
        /// </summary>
        public int phase_shift;

        /// <summary>
        /// if 1 then the resampling FIR filter will be linearly interpolated
        /// </summary>
        public int linear_interp;

        /// <summary>
        /// resampling cutoff frequency (swr: 6dB point; soxr: 0dB point). 1.0 corresponds to half the output sample rate
        /// </summary>
        public double cutoff;

        /// <summary>
        /// swr resampling filter type 
        /// </summary>
        public SwrFilterType filter_type;

        /// <summary>
        /// swr beta value for Kaiser window (only applicable if filter_type == AV_FILTER_TYPE_KAISER)
        /// </summary>
        public int kaiser_beta;

        /// <summary>
        /// soxr resampling precision (in bits)
        /// </summary>
        public double precision;

        /// <summary>
        /// soxr: if 1 then passband rolloff will be none (Chebyshev) & irrational ratio approximation precision will be higher
        /// </summary>
        public int cheby;

    public float min_compensation;                         //< swr minimum below which no compensation will happen
    public float min_hard_compensation;                    //< swr minimum below which no silence inject / sample drop will happen
    public float soft_compensation_duration;               //< swr duration over which soft compensation is applied
    public float max_soft_compensation;                    //< swr maximum soft compensation in seconds over soft_compensation_duration
    public float async;                                    //< swr simple 1 parameter async, similar to ffmpegs -async
    public Int64 firstpts_in_samples;                      //< swr first pts in samples

    public int resample_first;                             //< 1 if resampling must come first, 0 if rematrixing
    public int rematrix;                                   //< flag to indicate if rematrixing is needed (basically if input and output layouts mismatch)
    public int rematrix_custom;                            //< flag to indicate that a custom matrix has been defined

    public AudioData in;                                   //< input audio data
    public AudioData postin;                               //< post-input audio data: used for rematrix/resample
    public AudioData midbuf;                               //< intermediate audio data (postin/preout)
    public AudioData preout;                               //< pre-output audio data: used for rematrix/resample
    public AudioData out;                                  //< converted output audio data
    public AudioData in_buffer;                            //< cached audio data (convert and resample purpose)
    public AudioData silence;                              //< temporary with silence
    public AudioData drop_temp;                            //< temporary used to discard output
    public int in_buffer_index;                            //< cached buffer position
    public int in_buffer_count;                            //< cached buffer length
    public int resample_in_constraint;                     ///< 1 if the input end was reach before the output end, 0 otherwise
    public int flushed;                                    ///< 1 if data is to be flushed and no further input is expected
    public Int64 outpts;                                   //< output PTS
    public Int64 firstpts;                                 //< first PTS
    public int drop_output;                                //< number of output samples to drop

    public  AudioConvert *in_convert;                //< input conversion context
    public  AudioConvert *out_convert;               //< output conversion context
    public  AudioConvert *full_convert;              //< full conversion context (single conversion for input and output)
    public  ResampleContext *resample;               //< resampling context
    public  Resampler const *resampler;              //< resampler virtual function table

    public float matrix[SWR_CH_MAX][SWR_CH_MAX];           //< floating point rematrixing coefficients
    public uint8_t *native_matrix;
    public uint8_t *native_one;
    public uint8_t *native_simd_one;
    public uint8_t *native_simd_matrix;
    public int32_t matrix32[SWR_CH_MAX][SWR_CH_MAX];       //< 17.15 fixed point rematrixing coefficients
    public uint8_t matrix_ch[SWR_CH_MAX][SWR_CH_MAX+1];    //< Lists of input channels per output channel that have non zero rematrixing coefficients
    public mix_1_1_func_type *mix_1_1_f;
    public mix_1_1_func_type *mix_1_1_simd;

    public mix_2_1_func_type *mix_2_1_f;
    public mix_2_1_func_type *mix_2_1_simd;

    public mix_any_func_type *mix_any_f;

    /* TODO: callbacks for ASM optimizations */
}
    }