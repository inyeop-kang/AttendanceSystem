using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    public partial class FaceRegisterWindow : System.Windows.Window
    {
        private readonly Student student;
        private readonly WebcamCapture webcam;
        private DispatcherTimer previewTimer;
        private DispatcherTimer captureTimer;
        private List<string> capturedFrames;

        private const int TargetFrameCount = 12;
        private const int CaptureIntervalMilliseconds = 120;

        public FaceRegisterWindow(Student student)
        {
            InitializeComponent();
            this.student = student;
            webcam = new WebcamCapture();
            StudentInfoText.Text = student.Name + " (" + student.StudentNo + ") 얼굴 등록";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool started = webcam.Start();

            if (!started)
            {
                StatusText.Text = "웹캠을 열 수 없습니다.";
                CaptureButton.IsEnabled = false;
                return;
            }

            previewTimer = new DispatcherTimer();
            previewTimer.Interval = TimeSpan.FromMilliseconds(33);
            previewTimer.Tick += PreviewTimer_Tick;
            previewTimer.Start();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            Mat frame = webcam.ReadFrame();

            if (frame == null)
            {
                return;
            }

            PreviewImage.Source = webcam.ToBitmapSource(frame);
            frame.Dispose();
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureButton.IsEnabled = false;
            capturedFrames = new List<string>();
            StatusText.Text = "촬영 중... (0/" + TargetFrameCount + ")";

            captureTimer = new DispatcherTimer();
            captureTimer.Interval = TimeSpan.FromMilliseconds(CaptureIntervalMilliseconds);
            captureTimer.Tick += CaptureTimer_Tick;
            captureTimer.Start();
        }

        private async void CaptureTimer_Tick(object sender, EventArgs e)
        {
            Mat frame = webcam.ReadFrame();

            if (frame != null)
            {
                string base64 = webcam.ToBase64Jpeg(frame);
                capturedFrames.Add(base64);
                frame.Dispose();
            }

            StatusText.Text = "촬영 중... (" + capturedFrames.Count + "/" + TargetFrameCount + ")";

            if (capturedFrames.Count >= TargetFrameCount)
            {
                captureTimer.Stop();
                await SendCapturedFrames();
            }
        }

        private async Task SendCapturedFrames()
        {
            StatusText.Text = "서버로 전송 중...";

            FaceRegisterResponse response;

            try
            {
                response = await ApiClient.RegisterFace(student.Id, capturedFrames);
            }
            catch
            {
                StatusText.Text = "서버 전송에 실패했습니다.";
                MessageBox.Show(StatusText.Text, "연결 오류");
                CaptureButton.IsEnabled = true;
                return;
            }

            StatusText.Text = response.Message + " (유효 프레임 " + response.ValidFrameCount + "/" + response.TotalFrameCount + ")";

            if (response.Success)
            {
                MessageBox.Show(response.Message, "얼굴 등록 완료");
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(response.Message + "\n다시 촬영해 주세요.", "얼굴 등록 실패");
                CaptureButton.IsEnabled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (previewTimer != null)
            {
                previewTimer.Stop();
            }

            if (captureTimer != null)
            {
                captureTimer.Stop();
            }

            webcam.Stop();
        }
    }
}
