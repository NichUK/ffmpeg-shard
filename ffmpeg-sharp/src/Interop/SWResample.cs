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
    public unsafe class SWResample
    {
        public const string SWRESAMPLE_DLL_NAME = "swresample-0.dll";

        //#if LIBSWRESAMPLE_VERSION_MAJOR < 1
        /// <summary>
        /// Maximum number of channels    
        /// </summary>
        public const int SWR_CH_MAX  = 32;   
        //#endif
        public const int NS_TAPS = 20;

        private const int SWR_FLAG_RESAMPLE = 1; //< Force resampling even if equal sample rate
        //TODO use int resample ?
        //long term TODO can we enable this dynamically?

        /// <summary>
        /// Get the AVClass for swrContext. It can be used in combination with
        /// AV_OPT_SEARCH_FAKE_OBJ for examining options.
        /// @see av_opt_find().
        /// </summary>
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern AVClass* swr_get_class();

        /// <summary>
        /// Allocate SwrContext.
        /// 
        /// If you use this function you will need to set the parameters (manually or
        /// with swr_alloc_set_opts()) before calling swr_init().
        /// 
        /// @see swr_alloc_set_opts(), swr_init(), swr_free()
        /// @return NULL on error, allocated context otherwise
        /// </summary>
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        //public static extern SwrContext* swr_alloc();
        internal static extern IntPtr swr_alloc();

        public static SwrContext SwrAlloc()
        {
            var intPtr = swr_alloc();
            return (SwrContext)Marshal.PtrToStructure (intPtr, typeof (SwrContext));
        }

        /// <summary>
        /// Initialize context after user parameters have been set.
        /// 
        /// @return AVERROR error code in case of failure.
        /// </summary>
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_init(ref SwrContext s);

/**
 * Allocate SwrContext if needed and set/reset common parameters.
 *
 * This function does not require s to be allocated with swr_alloc(). On the
 * other hand, swr_alloc() can use swr_alloc_set_opts() to set the parameters
 * on the allocated context.
 *
 * @param s               Swr context, can be NULL
 * @param out_ch_layout   output channel layout (AV_CH_LAYOUT_*)
 * @param out_sample_fmt  output sample format (AV_SAMPLE_FMT_*).
 * @param out_sample_rate output sample rate (frequency in Hz)
 * @param in_ch_layout    input channel layout (AV_CH_LAYOUT_*)
 * @param in_sample_fmt   input sample format (AV_SAMPLE_FMT_*).
 * @param in_sample_rate  input sample rate (frequency in Hz)
 * @param log_offset      logging level offset
 * @param log_ctx         parent logging context, can be NULL
 *
 * @see swr_init(), swr_free()
 * @return NULL on error, allocated context otherwise
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        //return SwrContext*
        public static extern IntPtr swr_alloc_set_opts(ref SwrContext s,
                                      Int64 out_ch_layout, AVSampleFormat out_sample_fmt, int out_sample_rate,
                                      Int64  in_ch_layout, AVSampleFormat  in_sample_fmt, int  in_sample_rate,
                                      int log_offset, void *log_ctx);

/**
 * Free the given SwrContext and set the pointer to NULL.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void swr_free(ref IntPtr s);

/**
 * Convert audio.
 *
 * in and in_count can be set to 0 to flush the last few samples out at the
 * end.
 *
 * If more input is provided than output space then the input will be buffered.
 * You can avoid this buffering by providing more output space than input.
 * Convertion will run directly without copying whenever possible.
 *
 * @param s         allocated Swr context, with parameters set
 * @param out       output buffers, only the first one need be set in case of packed audio
 * @param out_count amount of space available for output in samples per channel
 * @param in        input buffers, only the first one need to be set in case of packed audio
 * @param in_count  number of input samples available in one channel
 *
 * @return number of samples output per channel, negative value on error
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_convert(ref SwrContext s, Byte **@out, int out_count,
                                Byte **@in , int in_count);

/**
 * Convert the next timestamp from input to output
 * timestamps are in 1/(in_sample_rate * out_sample_rate) units.
 *
 * @note There are 2 slightly differently behaving modes.
 *       First is when automatic timestamp compensation is not used, (min_compensation >= FLT_MAX)
 *              in this case timestamps will be passed through with delays compensated
 *       Second is when automatic timestamp compensation is used, (min_compensation < FLT_MAX)
 *              in this case the output timestamps will match output sample numbers
 *
 * @param pts   timestamp for the next input sample, INT64_MIN if unknown
 * @return the output timestamp for the next output sample
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Int64 swr_next_pts(ref SwrContext s, Int64 pts);

/**
 * Activate resampling compensation.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_set_compensation(ref SwrContext s, int sample_delta, int compensation_distance);

/**
 * Set a customized input channel mapping.
 *
 * @param s           allocated Swr context, not yet initialized
 * @param channel_map customized input channel mapping (array of channel
 *                    indexes, -1 for a muted channel)
 * @return AVERROR error code in case of failure.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_set_channel_mapping(ref SwrContext s, int *channel_map);

/**
 * Set a customized remix matrix.
 *
 * @param s       allocated Swr context, not yet initialized
 * @param matrix  remix coefficients; matrix[i + stride * o] is
 *                the weight of input channel i in output channel o
 * @param stride  offset between lines of the matrix
 * @return  AVERROR error code in case of failure.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_set_matrix(ref SwrContext s, double *matrix, int stride);

/**
 * Drops the specified number of output samples.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_drop_output(ref SwrContext s, int count);

/**
 * Injects the specified number of silence samples.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int swr_inject_silence(ref SwrContext s, int count);

/**
 * Gets the delay the next input sample will experience relative to the next output sample.
 *
 * Swresample can buffer data if more input has been provided than available
 * output space, also converting between sample rates needs a delay.
 * This function returns the sum of all such delays.
 * The exact delay is not necessarily an integer value in either input or
 * output sample rate. Especially when downsampling by a large value, the
 * output sample rate may be a poor choice to represent the delay, similarly
 * for upsampling and the input sample rate.
 *
 * @param s     swr context
 * @param base  timebase in which the returned delay will be
 *              if its set to 1 the returned delay is in seconds
 *              if its set to 1000 the returned delay is in milli seconds
 *              if its set to the input sample rate then the returned delay is in input samples
 *              if its set to the output sample rate then the returned delay is in output samples
 *              an exact rounding free delay can be found by using LCM(in_sample_rate, out_sample_rate)
 * @returns     the delay in 1/base units.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Int64 swr_get_delay(ref SwrContext s, Int64 @base);

/**
 * Return the LIBSWRESAMPLE_VERSION_INT constant.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern uint swresample_version();

/**
 * Return the swr build-time configuration.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern char *swresample_configuration();

/**
 * Return the swr license.
 */
        [DllImport(SWRESAMPLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern char *swresample_license();

/**
 * @}
 */

        /* SWRESAMPLE_SWRESAMPLE_H */

    
    }
}
