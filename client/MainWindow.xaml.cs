using System.Windows;
using AttendanceClient.Views;

namespace AttendanceClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentArea.Content = new AttendanceTodayView();
            SetActiveNavigation(true);
        }

        private void SetActiveNavigation(bool attendance)
        {
            var selected = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#2E4F6E");
            AttendanceTodayButton.Background = attendance ? selected : (System.Windows.Media.Brush)FindResource("Navy");
            StudentsButton.Background = attendance ? (System.Windows.Media.Brush)FindResource("Navy") : selected;
        }

        private void StudentsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new StudentsView();
            SetActiveNavigation(false);
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
            SetActiveNavigation(true);
        }
    }
}
