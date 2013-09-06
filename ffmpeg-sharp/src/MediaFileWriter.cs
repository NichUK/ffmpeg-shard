using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FFmpegSharp.Interop;
using FFmpegSharp.Interop.Format;
using System.IO;
using FFmpegSharp.Interop.Codec;
using System.Collections.ObjectModel;
using FFmpegSharp.Interop.Format.Context;
using FFmpegSharp.Interop.Format.Output;
using FFmpegSharp.Interop.SWResample;
using FFmpegSharp.Interop.SWScale;
using FFmpegSharp.Interop.Util;

namespace FFmpegSharp
{
    public unsafe class MediaFileWriter
    {
        internal AVFormatContext FormatContext;
        private bool _disposed = false;
        private SortedList<int, DecoderStream> _streams;
        private string _filepath;

        // 5 seconds stream duration 
        const double STREAM_DURATION = 200.0;
        const int STREAM_FRAME_RATE = 25;

        /* 25 images/s */
        private const int STREAM_NB_FRAMES = ((int)(STREAM_DURATION * STREAM_FRAME_RATE));

        // default pix_fmt
        const PixelFormat STREAM_PIX_FMT = PixelFormat.PIX_FMT_YUV420P;

        static SwsFlags sws_flags = SwsFlags.Bicubic;


        #region Properties

        public unsafe ReadOnlyCollection<DecoderStream> Streams
        {
            get { return new ReadOnlyCollection<DecoderStream>(_streams.Values); }
        }

        public string Filename
        {
            get { return _filepath; }
        }

        public long Length
        {
            get { return FormatContext.duration; }
        }

        public string FileFormat
        {
            get { unsafe { return FormatContext.iformat->name; } }
        }

        /// <summary>
        /// Duration of the stream
        /// </summary>
        public TimeSpan Duration
        {
            get { return new TimeSpan((long)(RawDuration * 1e7)); }
        }

        public double RawDuration
        {
            get
            {
                double duration = (double)(FormatContext.duration / (double)FFmpeg.AV_TIME_BASE);
                if (duration < 0)
                    duration = 0;
                return duration;
            }
        }

        #endregion

        static MediaFileWriter()
        {
            // Register all codecs and protocols
            FFmpeg.av_register_all();
#if DEBUG
            FFmpeg.av_log_set_level(1000);
#endif
        }

        public unsafe MediaFileWriter(string filepath, string outputFormatType = null)
        {
            AVError ret;

            if (String.IsNullOrEmpty(filepath))
                throw new ArgumentNullException("filepath");

            _filepath = filepath;

            AVFormatContext* outputContext;
            AVOutputFormat* outputFormat;


            // allocate the output media context
            var iRet = FFmpeg.avformat_alloc_output_context2(out outputContext, null, null, filepath);
            if (iRet < 0)
            {
                Debug.WriteLine("Could not deduce output format from file extension: using MPEG.");
                if (FFmpeg.avformat_alloc_output_context2(out outputContext, null, "mpeg", filepath) < 0)
                    throw new EncoderException("Could not alloc output context");
            }
            outputFormat = outputContext->oformat;

            AVCodec* videoCodec;
            AVCodec* audioCodec;

            // Add the audio and video streams using the default format codecs and initialize the codecs.
            if (outputFormat->video_codec == AVCodecID.CODEC_ID_NONE)
                throw new EncoderException("No Video Codec specified");

            //set up picture
            AVPicture src_picture;
            AVPicture dst_picture;


            if (outputFormat->audio_codec == AVCodecID.CODEC_ID_NONE)
                throw new EncoderException("No Audio Codec specified");

            // set the rest of the parameters for each stream, then open the audio and
            // video codecs and allocate the necessary encode buffers.
            AVStream* videoStream = AddStream(outputContext, &videoCodec, outputFormat->video_codec);
            OpenVideo(outputContext, videoCodec, videoStream, ref src_picture, ref dst_picture);
            
            //fixed (AVPicture* sourcePicturePtr = &src_picture)
            //fixed (AVPicture* destPicturePtr = &dst_picture)
            //{

            //var audioStream = AddStream(ref outputContext, ref audioCodec, outputFormat.audio_codec);
            //OpenAudio(ref outputContext, ref audioCodec, ref audioStream);

            FFmpeg.av_dump_format(outputContext, 0, filepath, 1);

            /* open the output file, if needed */
            if (!Convert.ToBoolean(outputFormat->flags & FFmpeg.AVFMT_NOFILE))
            {
                ret = FFmpeg.avio_open(&outputContext->pb, filepath, AvioFlags.AVIO_FLAG_WRITE);
                if (ret != AVError.OK)
                    throw new EncoderException(String.Format("Could not open '{0}': {1}", filepath, ret.ToString()));
            }

            /* Write the stream header, if any. */
            ret = FFmpeg.avformat_write_header(outputContext, null);
            if (ret != AVError.OK)
                throw new EncoderException("Error occurred when opening output file: {0}", ret.ToString());

            if (frame != null)
                frame->pts = 0;
            for (; ; )
            {
                /* Compute current audio and video time. */
                //audio_time = audio_st ? audio_st->pts.val * av_q2d(audio_st->time_base) : 0.0;
                var videoTime = 0.0;
                if (videoStream != null)
                {
                    var pts = videoStream->pts;
                    var timebase = videoStream->time_base.ToDouble();
                    videoTime = pts.val * timebase;
                }

                if //((!audio_st || audio_time >= STREAM_DURATION) &&
                    (videoStream == null || videoTime >= STREAM_DURATION) //)
                    break;

                //* write interleaved audio and video frames */
                //if (videoStreamP == null || (video_st && audio_st && audio_time < videoTime))
                //{
                //    write_audio_frame(oc, audio_st);
                //}
                //else
                //{

                //WriteVideoFrame(ref outputContext, &videoStream, ref src_picture, ref dst_picture);
                WriteVideoFrame(outputContext, videoStream, &src_picture, &dst_picture);
                //WriteVideoFrame(ref outputContext, &videoStream, sourcePicturePtr, destPicturePtr);
                frame->pts += FFmpeg.av_rescale_q(1, videoStream->codec->time_base, videoStream->time_base);
                //}
            }

            /* Write the trailer, if any. The trailer must be written before you
         * close the CodecContexts open when you wrote the header; otherwise
         * av_write_trailer() may try to use memory that was freed on
         * av_codec_close(). */
            FFmpeg.av_write_trailer(outputContext);

            /* Close each codec. */
            if (videoStream != null)
                CloseVideo(outputContext, videoStream, &src_picture, &dst_picture);
            //if (audio_st)
            //    close_audio(oc, audio_st);

            if (!Convert.ToBoolean(outputFormat->flags & FFmpeg.AVFMT_NOFILE))
                /* Close the output file. */
                FFmpeg.avio_close(outputContext->pb);

            /* free the stream */
            FFmpeg.avformat_free_context(outputContext);
            //}
        }


        private unsafe AVStream *AddStream(AVFormatContext* formatContextPtr, AVCodec** codec, AVCodecID codecId)
        {

            AVCodecContext* cc;
            AVStream* stream;   

            // find the encoder
            *codec = FFmpeg.avcodec_find_encoder(codecId);

            if (codec == null)
                throw new EncoderException(String.Format("Could not find encoder for '{0}'", FFmpeg.avcodec_get_name(codecId)));

            stream = FFmpeg.avformat_new_stream(formatContextPtr, *codec);
            if (stream == null)
                return null;

            stream->id = (int)formatContextPtr->nb_streams - 1;
            cc = stream->codec;

            switch ((*codec)->type)
            {
                case AVMediaType.AVMEDIA_TYPE_AUDIO:
                    cc->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
                    cc->bit_rate = 64000;
                    cc->sample_rate = 44100;
                    cc->channels = 2;
                    break;

                case AVMediaType.AVMEDIA_TYPE_VIDEO:
                    cc->codec_id = codecId;
                    cc->bit_rate = 400000;

                    // Resolution must be a multiple of two.
                    cc->width = 352;
                    cc->height = 288;

                    // timebase: This is the fundamental unit of time (in seconds) in terms
                    // of which frame timestamps are represented. For fixed-fps content,
                    // timebase should be 1/framerate and timestamp increments should be
                    // identical to 1.
                    cc->time_base.den = STREAM_FRAME_RATE;
                    cc->time_base.num = 1;
                    // emit one intra frame every twelve frames at most
                    cc->gop_size = 12;
                    cc->pix_fmt = STREAM_PIX_FMT;
                    if (cc->codec_id == AVCodecID.CODEC_ID_MPEG2VIDEO)
                    {
                        // just for testing, we also add B frames
                        cc->max_b_frames = 2;
                    }
                    if (cc->codec_id == AVCodecID.CODEC_ID_MPEG1VIDEO)
                    {
                        /* Needed to avoid using macroblocks in which some coeffs overflow.             
                         * * This does not happen with normal video, it just happens here as             
                         * * the motion of the chroma plane does not match the luma plane. */
                        cc->mb_decision = 2;
                    }
                    break;

                default:
                    break;
            }

            // Some formats want stream headers to be separate.
            if (Convert.ToBoolean(((uint)formatContextPtr->oformat->flags) & FFmpeg.AVFMT_GLOBALHEADER))
                cc->flags |= (CODEC_FLAG)FFmpeg.CODEC_FLAG_GLOBAL_HEADER;

            return stream;
        }

        // video output
        static AVFrame* frame;
        static int frame_count;

        static void OpenVideo(AVFormatContext* oc, AVCodec* codec, AVStream* st, ref AVPicture src_picture, ref AVPicture dst_picture)
        {
            AVError ret;
            AVCodecContext* c = st->codec;

            /* open the codec */
            ret = FFmpeg.avcodec_open2(c, codec, null);
            if (ret < 0)
                throw new EncoderException(String.Format("Could not open video codec: {0}", ret.ToString()));

            /* allocate and init a re-usable frame */
            frame = FFmpeg.avcodec_alloc_frame();
            if (frame == null)
                throw new EncoderException("Could not allocate video frame");

            /* Allocate the encoded raw picture. */
            ret = FFmpeg.avpicture_alloc(out dst_picture, c->pix_fmt, c->width, c->height);
            if (ret < 0)
                throw new EncoderException(String.Format("Could not allocate picture: {0}", ret.ToString()));

            /* If the output format is not YUV420P, then a temporary YUV420P
             * picture is needed too. It is then converted to the required
             * output format. */
            if (c->pix_fmt != PixelFormat.PIX_FMT_YUV420P)
            {
                ret = FFmpeg.avpicture_alloc(out src_picture, PixelFormat.PIX_FMT_YUV420P, c->width, c->height);
                if (ret < 0)
                    throw new EncoderException(String.Format("Could not allocate temporary picture: {0}", ret.ToString()));
            }

            /* copy data and linesize picture pointers to frame */
            *((AVPicture*)frame) = dst_picture;
        }

        ///* Prepare a dummy image. */
        static void fill_yuv_image(AVPicture* pict, int frame_index, int width, int height)
        {
            int x, y, i;

            i = frame_index;

            /* Y */
            for (y = 0; y < height; y++)
                for (x = 0; x < width; x++)
                    *(&pict->data[0]+(y * pict->linesize[0] + x)) = x + y + i * 3;

            /* Cb and Cr */
            for (y = 0; y < height / 2; y++)
            {
                for (x = 0; x < width / 2; x++)
                {
                    *(&pict->data[1]+(y * pict->linesize[1] + x)) = 128 + y + i * 2;
                    *(&pict->data[2]+(y * pict->linesize[2] + x)) = 64 + x + i * 5;
                }
            }
        }

        static unsafe void WriteVideoFrame(AVFormatContext* oc, AVStream* st, AVPicture* src_picture, AVPicture* dst_picture)
        {
            AVError ret;
            SwsContext* sws_ctx = null;
            AVCodecContext* c = st->codec;

            if (frame_count >= STREAM_NB_FRAMES)
            {
                /* No more frames to compress. The codec has a latency of a few
                 * frames if using B-frames, so we get the last frames by
                 * passing the same picture again. */
            }
            else
            {
                if (c->pix_fmt != PixelFormat.PIX_FMT_YUV420P)
                {
                    /* as we only generate a YUV420P picture, we must convert it
                     * to the codec pixel format if needed */
                    sws_ctx = FFmpeg.sws_getContext(c->width, c->height, PixelFormat.PIX_FMT_YUV420P,
                                                c->width, c->height, c->pix_fmt,
                                                sws_flags, null, null, null);
                    if (sws_ctx == null)
                        throw new EncoderException("Could not initialize the conversion context");


                    //fill_yuv_image(ref src_picture, frame_count, c->width, c->height);
                    fill_yuv_image(src_picture, frame_count, c->width, c->height);

                    //FFmpeg.sws_scale(sws_ctx, (byte**)src_picture.data, src_picture.linesize,
                    //          0, c->height, (byte**)dst_picture.data, dst_picture.linesize);

                    //FFmpeg.sws_scale(sws_ctx, (byte**)src_picture.data, src_picture.linesize,
                    //    0, c->height, (byte**)dst_picture.data, dst_picture.linesize);

                    AVPicture source = *src_picture;
                    AVPicture dest = *dst_picture;
                    Byte* sourceData = (Byte*)source.data;
                    Byte* destData = (Byte*)dest.data;

                    FFmpeg.sws_scale(sws_ctx, &sourceData, src_picture->linesize,
                        0, c->height, &destData, dst_picture->linesize);
                }
                else
                {
                    fill_yuv_image(dst_picture, frame_count, c->width, c->height);
                }
            }

            if (Convert.ToBoolean(oc->oformat->flags & FFmpeg.AVFMT_RAWPICTURE))
            {
                /* Raw video case - directly store the picture in the packet */
                var pkt = new AVPacket();
                FFmpeg.av_init_packet(ref pkt);

                pkt.flags |= PacketFlags.Key;
                pkt.stream_index = st->index;
                pkt.data = new IntPtr(dst_picture->data[0]);
                pkt.size = sizeof(AVPicture);

                ret = FFmpeg.av_interleaved_write_frame(oc, ref pkt);
            }
            else
            {
                var pkt = new AVPacket();
                bool got_packet;
                FFmpeg.av_init_packet(ref pkt);

                /* encode the image */
                ret = FFmpeg.avcodec_encode_video2(c, &pkt, frame, &got_packet);
                if (ret < 0)
                    throw new EncoderException("Error encoding video frame: {0}", ret.ToString());

                /* If size is zero, it means the image was buffered. */
                if (Convert.ToBoolean(ret != AVError.OK) && got_packet && (pkt.size > 0))
                {
                    pkt.stream_index = st->index;

                    /* Write the compressed frame to the media file. */
                    ret = FFmpeg.av_interleaved_write_frame(oc, ref pkt);
                }
                else
                {
                    ret = 0;
                }
            }
            if (ret != 0)
                throw new EncoderException("Error while writing video frame: {0}", ret.ToString());

            frame_count++;
        }

        static void CloseVideo(AVFormatContext* oc, AVStream* st, AVPicture* src_picture, AVPicture* dst_picture)
        {
            FFmpeg.avcodec_close(ref *st->codec);
            FFmpeg.av_free(src_picture->data);
            FFmpeg.av_free(dst_picture->data);
            FFmpeg.av_free(frame);
        }


        // audio output

        static float t, tincr, tincr2;

        static Byte** src_samples_data;
        static int src_samples_linesize;
        static int src_nb_samples;

        static int max_dst_nb_samples;
        Byte** dst_samples_data;
        int dst_samples_linesize;
        int dst_samples_size;

        static SwrContext swr_ctx;

        //static void OpenAudio(ref AVFormatContext oc, ref AVCodec codec, ref AVStream st)
        //{
        //    AVCodecContext *c;
        //    AVError ret;

        //    c = st.codec;

        //    /* open it */
        //    ret = FFmpeg.avcodec_open2(c, ref codec, null);
        //    if (ret < 0)
        //        throw new EncoderException("Could not open audio codec: {0}",ret.ToString());

        //    /* init signal generator */
        //    t     = 0;
        //    tincr = (float) (2 * Math.PI * 110.0 / c->sample_rate);
        //    /* increment frequency by 110 Hz per second */
        //    tincr2 = (float)(2 * Math.PI * 110.0 / c->sample_rate / c->sample_rate);

        //    src_nb_samples = Convert.ToBoolean((CODEC_CAP)c->codec->capabilities & CODEC_CAP.VariableFrameSize) ?
        //        10000 : c->frame_size;

        //    ret = FFmpeg.av_samples_alloc_array_and_samples(ref src_samples_data, ref src_samples_linesize, c->channels,
        //        src_nb_samples, c->sample_fmt, 0);

        //    if (ret < 0)
        //        throw new EncoderException("Could not allocate source samples: {0}",ret.ToString());

        //    /* create resampler context */
        //    if (c->sample_fmt != AVSampleFormat.AV_SAMPLE_FMT_S16)
        //    {
        //        swr_ctx = SWResample.SwrAlloc();
        //        if (swr_ctx.Equals(new SwrContext()))
        //            throw new EncoderException("Could not allocate resampler context")

        //        /* set options */
        //        //av_opt_set_int       (swr_ctx, "in_channel_count",   c->channels,       0);
        //        set_number()
        //        av_opt_set_int       (swr_ctx, "in_sample_rate",     c->sample_rate,    0);
        //        av_opt_set_sample_fmt(swr_ctx, "in_sample_fmt",      AV_SAMPLE_FMT_S16, 0);
        //        av_opt_set_int       (swr_ctx, "out_channel_count",  c->channels,       0);
        //        av_opt_set_int       (swr_ctx, "out_sample_rate",    c->sample_rate,    0);
        //        av_opt_set_sample_fmt(swr_ctx, "out_sample_fmt",     c->sample_fmt,     0);

        //        /* initialize the resampling context */
        //        if ((ret = swr_init(swr_ctx)) < 0) {
        //            fprintf(stderr, "Failed to initialize the resampling context\n");
        //            exit(1);
        //        }
        //    }

        //    /* compute the number of converted samples: buffering is avoided
        //    * ensuring that the output buffer will contain at least all the
        //    * converted input samples */
        //    max_dst_nb_samples = src_nb_samples;
        //    ret = av_samples_alloc_array_and_samples(&dst_samples_data, &dst_samples_linesize, c->channels,
        //        max_dst_nb_samples, c->sample_fmt, 0);
        //    if (ret < 0) {
        //        fprintf(stderr, "Could not allocate destination samples\n");
        //        exit(1);
        //    }
        //    dst_samples_size = av_samples_get_buffer_size(NULL, c->channels, max_dst_nb_samples,
        //        c->sample_fmt, 0);
        //}

        ///* Prepare a 16 bit dummy audio frame of 'frame_size' samples and
        //* 'nb_channels' channels. */
        //static void get_audio_frame(int16_t *samples, int frame_size, int nb_channels)
        //{
        //    int j, i, v;
        //    int16_t *q;

        //    q = samples;
        //    for (j = 0; j < frame_size; j++) {
        //        v = (int)(sin(t) * 10000);
        //        for (i = 0; i < nb_channels; i++)
        //            *q++ = v;
        //        t     += tincr;
        //        tincr += tincr2;
        //    }
        //}

        //static void write_audio_frame(AVFormatContext *oc, AVStream *st)
        //{
        //    AVCodecContext *c;
        //    AVPacket pkt = { 0 }; // data and size must be 0;
        //    AVFrame *frame = avcodec_alloc_frame();
        //    int got_packet, ret, dst_nb_samples;

        //    av_init_packet(&pkt);
        //    c = st->codec;

        //    get_audio_frame((int16_t *)src_samples_data[0], src_nb_samples, c->channels);

        //    /* convert samples from native format to destination codec format, using the resampler */
        //    if (swr_ctx) {
        //        /* compute destination number of samples */
        //        dst_nb_samples = av_rescale_rnd(swr_get_delay(swr_ctx, c->sample_rate) + src_nb_samples,
        //            c->sample_rate, c->sample_rate, AV_ROUND_UP);
        //        if (dst_nb_samples > max_dst_nb_samples) {
        //            av_free(dst_samples_data[0]);
        //            ret = av_samples_alloc(dst_samples_data, &dst_samples_linesize, c->channels,
        //                dst_nb_samples, c->sample_fmt, 0);
        //            if (ret < 0)
        //                exit(1);
        //            max_dst_nb_samples = dst_nb_samples;
        //            dst_samples_size = av_samples_get_buffer_size(NULL, c->channels, dst_nb_samples,
        //                c->sample_fmt, 0);
        //        }

        //        /* convert to destination format */
        //        ret = swr_convert(swr_ctx,
        //            dst_samples_data, dst_nb_samples,
        //            (const uint8_t **)src_samples_data, src_nb_samples);
        //        if (ret < 0) {
        //            fprintf(stderr, "Error while converting\n");
        //            exit(1);
        //        }
        //    } else {
        //        dst_samples_data[0] = src_samples_data[0];
        //        dst_nb_samples = src_nb_samples;
        //    }

        //    frame->nb_samples = dst_nb_samples;
        //    avcodec_fill_audio_frame(frame, c->channels, c->sample_fmt,
        //        dst_samples_data[0], dst_samples_size, 0);

        //    ret = avcodec_encode_audio2(c, &pkt, frame, &got_packet);
        //    if (ret < 0) {
        //        fprintf(stderr, "Error encoding audio frame: %s\n", av_err2str(ret));
        //        exit(1);
        //    }

        //    if (!got_packet)
        //        return;

        //    pkt.stream_index = st->index;

        //    /* Write the compressed frame to the media file. */
        //    ret = av_interleaved_write_frame(oc, &pkt);
        //    if (ret != 0) {
        //        fprintf(stderr, "Error while writing audio frame: %s\n",
        //            av_err2str(ret));
        //        exit(1);
        //    }
        //    avcodec_free_frame(&frame);
        //}

        //static void close_audio(AVFormatContext *oc, AVStream *st)
        //{
        //    avcodec_close(st->codec);
        //    av_free(src_samples_data[0]);
        //    av_free(dst_samples_data[0]);
        //}

    }
}
