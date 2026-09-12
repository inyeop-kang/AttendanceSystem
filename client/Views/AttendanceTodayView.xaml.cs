using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    public partial class AttendanceTodayView : UserControl
    {
        public AttendanceTodayView()
        {
            InitializeComponent();
        }

        private async void AttendanceTodayView_Loaded(object sender, RoutedEventArgs e)
        {
            GreetingText.Text = "안녕하세요, " + Session.AdminUsername + "님";
            DateText.Text = DateTime.Now.ToString("yyyy년 MM월 dd일 (ddd) HH:mm");

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;

            await LoadSummary();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSummary();
        }

        private async Task LoadSummary()
        {
            DateTime startDate = DateTime.Today;
            if (StartDatePicker.SelectedDate.HasValue)
            {
                startDate = StartDatePicker.SelectedDate.Value;
            }

            DateTime endDate = DateTime.Today;
            if (EndDatePicker.SelectedDate.HasValue)
            {
                endDate = EndDatePicker.SelectedDate.Value;
            }

            TodaySummary summary;

            try
            {
                summary = await ApiClient.GetAttendanceSummary(startDate, endDate);
            }
            catch
            {
                MessageBox.Show("출석 현황을 가져오지 못했습니다.", "오류");
                return;
            }

            PresentCountText.Text = summary.PresentCount.ToString();
            LateCountText.Text = summary.LateCount.ToString();
            AbsentCountText.Text = summary.AbsentCount.ToString();
            TotalCountText.Text = summary.TotalCount.ToString();
            AttendanceGrid.ItemsSource = summary.Items;
        }
    }
}
