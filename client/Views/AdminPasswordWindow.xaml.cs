using System.Windows;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    public partial class AdminPasswordWindow : Window
    {
        public AdminPasswordWindow()
        {
            InitializeComponent();
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string password = PasswordBox.Password;

            MessageText.Text = "";

            if (string.IsNullOrEmpty(password))
            {
                MessageText.Text = "비밀번호를 입력해 주세요.";
                return;
            }

            LoginResponse response;

            try
            {
                response = await ApiClient.Login(Session.AdminUsername, password);
            }
            catch
            {
                MessageText.Text = "서버에 연결할 수 없습니다.";
                return;
            }

            if (response == null || !response.Success)
            {
                MessageText.Text = "비밀번호가 올바르지 않습니다.";
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
