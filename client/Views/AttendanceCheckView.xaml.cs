using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    public partial class AttendanceCheckView : UserControl
    {
        private readonly WebcamCapture webcam;
        private DispatcherTimer previewTimer;
        private DispatcherTimer captureTimer;
        private List<string> capturedFrames;
        private string captureMode;

        private const int CheckFrameCount = 5;
        private const int CheckCaptureIntervalMilliseconds = 150;

        public AttendanceCheckView()
        {
            InitializeComponent();
            webcam = new WebcamCapture();
        }

        private void AttendanceCheckView_Unloaded(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            bool started = webcam.Start();

            if (!started)
            {
                MessageBox.Show("웹캠을 열 수 없습니다.", "오류");
                return;
            }

            previewTimer = new DispatcherTimer();
            previewTimer.Interval = TimeSpan.FromMilliseconds(33);
            previewTimer.Tick += PreviewTimer_Tick;
            previewTimer.Start();

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            SetCaptureButtonsEnabled(true);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void SetCaptureButtonsEnabled(bool enabled)
        {
            CheckInButton.IsEnabled = enabled;
            CheckOutButton.IsEnabled = enabled;
        }

        private void StopCamera()
        {
            if (previewTimer != null)
            {
                previewTimer.Stop();
                previewTimer = null;
            }

            if (captureTimer != null)
            {
                captureTimer.Stop();
                captureTimer = null;
            }

            webcam.Stop();

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            SetCaptureButtonsEnabled(false);
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

        private void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            StartCapture("checkin");
        }

        private void CheckOutButton_Click(object sender, RoutedEventArgs e)
        {
            StartCapture("checkout");
        }

        private void StartCapture(string mode)
        {
            captureMode = mode;
            SetCaptureButtonsEnabled(false);
            capturedFrames = new List<string>();
            ResultMessageText.Text = "인식 중입니다... (0/" + CheckFrameCount + ")";

            captureTimer = new DispatcherTimer();
            captureTimer.Interval = TimeSpan.FromMilliseconds(CheckCaptureIntervalMilliseconds);
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

            ResultMessageText.Text = "인식 중입니다... (" + capturedFrames.Count + "/" + CheckFrameCount + ")";

            if (capturedFrames.Count >= CheckFrameCount)
            {
                captureTimer.Stop();
                await SendCapturedFrames();
            }
        }

        private async Task SendCapturedFrames()
        {
            if (capturedFrames.Count == 0)
            {
                MessageBox.Show("웹캠에서 영상을 가져올 수 없습니다.", "오류");
                SetCaptureButtonsEnabled(true);
                return;
            }

            AttendanceCheckResponse response;

            try
            {
                if (captureMode == "checkout")
                {
                    response = await ApiClient.CheckOut(capturedFrames);
                }
                else
                {
                    response = await ApiClient.CheckIn(capturedFrames);
                }
            }
            catch
            {
                ResultMessageText.Text = "서버 통신에 실패했습니다.";
                SetCaptureButtonsEnabled(true);
                return;
            }

            ShowResult(response);
            SetCaptureButtonsEnabled(true);
        }

        private void ShowResult(AttendanceCheckResponse response)
        {
            if (!response.Matched)
            {
                ResultNameText.Text = "인식 실패 / 미등록";
                ResultDeptText.Text = "";
                ResultConfidenceText.Text = "유사도: " + (response.Confidence * 100).ToString("F1") + "%";
                ResultStatusText.Text = "";
                ResultStatusText.Foreground = Brushes.Black;
                ResultMessageText.Text = response.Message;
                return;
            }

            ResultNameText.Text = response.Name;
            ResultDeptText.Text = response.Department + " " + response.Grade + "기 (" + response.StudentNo + ")";
            ResultConfidenceText.Text = "유사도: " + (response.Confidence * 100).ToString("F1") + "%";

            if (response.CheckOutTime != DateTime.MinValue)
            {
                ResultStatusText.Text = "퇴실";
                ResultStatusText.Foreground = Brushes.SteelBlue;
            }
            else
            {
                ResultStatusText.Text = "입실";
                ResultStatusText.Foreground = Brushes.Green;
            }

            ResultMessageText.Text = response.Message;
        }
    }
}
