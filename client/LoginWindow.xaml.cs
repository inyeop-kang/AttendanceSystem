using System.Windows;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            MessageText.Text = "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageText.Text = "아이디와 비밀번호를 입력해 주세요.";
                return;
            }

            LoginResponse response;

            try
            {
                response = await ApiClient.Login(username, password);
            }
            catch
            {
                MessageText.Text = "서버에 연결할 수 없습니다. 서버 실행 상태를 확인해 주세요.";
                Sound.PlayConnectionError();
                MessageBox.Show(MessageText.Text, "연결 오류");
                return;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                {
                    MessageText.Text = response.Message;
                }
                else
                {
                    MessageText.Text = "로그인에 실패했습니다.";
                }
                return;
            }

            Session.AdminUsername = response.AdminUsername;

            ModeSelectWindow modeSelectWindow = new ModeSelectWindow();
            modeSelectWindow.Show();
            Close();
        }
    }
}
