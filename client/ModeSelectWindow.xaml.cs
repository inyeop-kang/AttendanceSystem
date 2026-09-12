using System.Windows;
using AttendanceClient.Views;

namespace AttendanceClient
{
    public partial class ModeSelectWindow : Window
    {
        public ModeSelectWindow()
        {
            InitializeComponent();
        }

        private void AdminModeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void KioskModeButton_Click(object sender, RoutedEventArgs e)
        {
            KioskWindow kioskWindow = new KioskWindow();
            kioskWindow.Show();
            Close();
        }
    }
}
