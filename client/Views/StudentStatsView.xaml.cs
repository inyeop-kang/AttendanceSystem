using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    // 교육생 한 명을 골라 기간별 출결 통계를 보여주는 화면.
    public partial class StudentStatsView : UserControl
    {
        public StudentStatsView()
        {
            InitializeComponent();
        }

        private async void StudentStatsView_Loaded(object sender, RoutedEventArgs e)
        {
            // 기본 기간은 최근 30일로 잡는다.
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-29);
            EndDatePicker.SelectedDate = DateTime.Today;

            List<Student> students;

            try
            {
                students = await ApiClient.GetStudents(null, null);
            }
            catch
            {
                MessageBox.Show("서버에서 교육생 목록을 가져오지 못했습니다.", "오류");
                return;
            }

            StudentComboBox.ItemsSource = students;

            if (students.Count > 0)
            {
                StudentComboBox.SelectedIndex = 0;
                await LoadStats();
            }
            else
            {
                StudentInfoText.Text = "등록된 교육생이 없습니다.";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStats();
        }

        private async Task LoadStats()
        {
            Student student = StudentComboBox.SelectedItem as Student;

            if (student == null)
            {
                MessageBox.Show("교육생을 선택해 주세요.", "안내");
                return;
            }

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

            if (startDate > endDate)
            {
                MessageBox.Show("시작일이 종료일보다 늦습니다.", "안내");
                return;
            }

            StudentStatsResponse stats;

            try
            {
                stats = await ApiClient.GetStudentStats(student.Id, startDate, endDate);
            }
            catch
            {
                MessageBox.Show("개별 통계를 가져오지 못했습니다.", "오류");
                return;
            }

            if (!stats.Success)
            {
                MessageBox.Show(stats.Message, "오류");
                return;
            }

            StudentInfoText.Text = stats.Name + " · " + stats.StudentNo + " · " + stats.Department + " " + stats.Grade + "기";

            PresentDayText.Text = stats.PresentDayCount.ToString();
            AbsentDayText.Text = stats.AbsentDayCount.ToString();
            AttendanceRateText.Text = stats.AttendanceRate.ToString("0.#") + "%";
            AverageStayText.Text = stats.AverageStayDuration;

            AverageCheckInText.Text = stats.AverageCheckInTime;
            AverageCheckOutText.Text = stats.AverageCheckOutTime;
            TotalStayText.Text = stats.TotalStayDuration;
            MissingCheckOutText.Text = stats.MissingCheckOutCount + "일";

            // 출석률의 분모는 기간 안에서 실제로 출결 기록이 있었던 날(교육 진행일)이다.
            OperatingDayText.Text = "기간 내 교육일 " + stats.OperatingDayCount + "일 기준";

            StatsGrid.ItemsSource = stats.Items;
        }
    }
}
