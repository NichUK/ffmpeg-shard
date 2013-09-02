using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpegSharp.Interop.SWResample
{
    public class Enums
    {
        public enum SwrDitherType
        {
            SWR_DITHER_NONE = 0,
            SWR_DITHER_RECTANGULAR,
            SWR_DITHER_TRIANGULAR,
            SWR_DITHER_TRIANGULAR_HIGHPASS,

            SWR_DITHER_NS = 64,         ///< not part of API/ABI
            SWR_DITHER_NS_LIPSHITZ,
            SWR_DITHER_NS_F_WEIGHTED,
            SWR_DITHER_NS_MODIFIED_E_WEIGHTED,
            SWR_DITHER_NS_IMPROVED_E_WEIGHTED,
            SWR_DITHER_NS_SHIBATA,
            SWR_DITHER_NS_LOW_SHIBATA,
            SWR_DITHER_NS_HIGH_SHIBATA,
            SWR_DITHER_NB,              ///< not part of API/ABI
        };

        /** Resampling Engines */
        public enum SwrEngine
        {
            SWR_ENGINE_SWR,             /**< SW Resampler */
            SWR_ENGINE_SOXR,            /**< SoX Resampler */
            SWR_ENGINE_NB,              ///< not part of API/ABI
        };

        /** Resampling Filter Types */
        public enum SwrFilterType
        {
            SWR_FILTER_TYPE_CUBIC,              /**< Cubic */
            SWR_FILTER_TYPE_BLACKMAN_NUTTALL,   /**< Blackman Nuttall Windowed Sinc */
            SWR_FILTER_TYPE_KAISER,             /**< Kaiser Windowed Sinc */
        };

        public enum AVMatrixEncoding
        {
            AV_MATRIX_ENCODING_NONE, 
            AV_MATRIX_ENCODING_DOLBY, 
            AV_MATRIX_ENCODING_DPLII, 
            AV_MATRIX_ENCODING_NB
        } 
    }
}
