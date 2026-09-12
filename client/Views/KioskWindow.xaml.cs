using System.Windows;

namespace AttendanceClient.Views
{
    public partial class KioskWindow : Window
    {
        public KioskWindow()
        {
            InitializeComponent();
        }

        private void AdminModeButton_Click(object sender, RoutedEventArgs e)
        {
            AdminPasswordWindow passwordWindow = new AdminPasswordWindow();
            bool confirmed = passwordWindow.ShowDialog() == true;

            if (!confirmed)
            {
                return;
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }
    }
}
