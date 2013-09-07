using System;
using System.Drawing;
using System.Windows.Forms;
using FFmpegSharp.Interop.Codec;
using FFmpegSharp.Interop.Util;

namespace FFmpegSharp.Examples.VideoPlayer
{
    public partial class VideoPlayer : Form
    {
        public VideoPlayer()
        {
            InitializeComponent();
        }

        private void m_btnPlay_Click(object sender, EventArgs e)
        {
            MediaFileReader file = new MediaFileReader(m_txtPath.Text);

            foreach (DecoderStream stream in file.Streams)
            {
                var videoStream = stream as VideoDecoderStream;

                //for (var i = 0; i < 100; i++)
                //{
                //    if (videoStream != null)
                //    {
                //        var buffer = new byte[videoStream.FrameSize];
                //        var frameRead = videoStream.ReadFrame(out buffer);
                //        Bitmap frame = new Bitmap();
                //        //var image = new AVFrame()
                //        //image.data=
                //        //m_videoSurface.Parent.BackgroundImage
                //    }
                //}

                if (videoStream != null)
                    m_videoSurface.Stream = new VideoScalingStream(videoStream, m_videoSurface.ClientRectangle.Width,
                                                                   m_videoSurface.ClientRectangle.Height, PixelFormat.PIX_FMT_BGRA);
            }
        }

        private void m_btnFile_Click(object sender, EventArgs e)
        {
            DialogResult d = m_dlgFile.ShowDialog();

            if (d == DialogResult.OK)
                m_txtPath.Text = m_dlgFile.FileName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var mediaWriter = new MediaFileWriter("\\temp\\testwrite.mp4", null);
            

        }
    }
}
