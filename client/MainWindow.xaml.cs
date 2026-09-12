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
        }

        private void StudentsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new StudentsView();
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
        }
    }
}
