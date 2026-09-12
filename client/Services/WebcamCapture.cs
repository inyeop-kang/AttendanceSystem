using System;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;

namespace AttendanceClient.Services
{
    public class WebcamCapture
    {
        private VideoCapture capture;

        public bool Start()
        {
            capture = new VideoCapture(0);

            if (!capture.IsOpened())
            {
                capture = null;
                return false;
            }

            return true;
        }

        public void Stop()
        {
            if (capture != null)
            {
                capture.Release();
                capture.Dispose();
                capture = null;
            }
        }

        public bool IsRunning()
        {
            if (capture == null)
            {
                return false;
            }

            return true;
        }

        public Mat ReadFrame()
        {
            if (capture == null)
            {
                return null;
            }

            Mat frame = new Mat();
            bool ok = capture.Read(frame);

            if (!ok || frame.Empty())
            {
                frame.Dispose();
                return null;
            }

            return frame;
        }

        public BitmapSource ToBitmapSource(Mat frame)
        {
            BitmapSource source = frame.ToBitmapSource();
            source.Freeze();
            return source;
        }

        public string ToBase64Jpeg(Mat frame)
        {
            byte[] bytes = frame.ToBytes(".jpg");
            string base64 = Convert.ToBase64String(bytes);
            return base64;
        }
    }
}
