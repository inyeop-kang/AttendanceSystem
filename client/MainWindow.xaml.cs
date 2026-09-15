using System.Windows;
using System.Windows.Controls;
using AttendanceClient.Views;

namespace AttendanceClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentArea.Content = new AttendanceTodayView();
            SetActiveNavigation(AttendanceTodayButton);
        }

        // 선택된 메뉴 버튼만 밝은 남색으로 표시한다.
        private void SetActiveNavigation(Button selectedButton)
        {
            var selected = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2E4F6E");
            var normal = (System.Windows.Media.Brush)FindResource("Navy");

            AttendanceTodayButton.Background = selectedButton == AttendanceTodayButton ? selected : normal;
            StudentsButton.Background = selectedButton == StudentsButton ? selected : normal;
            StudentStatsButton.Background = selectedButton == StudentStatsButton ? selected : normal;
        }

        private void StudentsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new StudentsView();
            SetActiveNavigation(StudentsButton);
        }

        private void StudentStatsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new StudentStatsView();
            SetActiveNavigation(StudentStatsButton);
        }

        private void AttendanceCheckButton_Click(object sender, RoutedEventArgs e)
        {
            KioskWindow kioskWindow = new KioskWindow();
            kioskWindow.Show();
            Close();
        }

        private void AttendanceTodayButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new AttendanceTodayView();
            SetActiveNavigation(AttendanceTodayButton);
        }
    }
}
