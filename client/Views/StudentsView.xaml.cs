using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    public partial class StudentsView : UserControl
    {
        public StudentsView()
        {
            InitializeComponent();
        }

        private async void StudentsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStudents(null);
        }

        private async Task LoadStudents(string keyword)
        {
            try
            {
                List<Student> list = await ApiClient.GetStudents(keyword, null);
                StudentsGrid.ItemsSource = list;
            }
            catch
            {
                MessageBox.Show("서버에서 교육생 목록을 가져오지 못했습니다.", "오류");
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStudents(SearchBox.Text);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStudents(null);
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            StudentEditWindow window = new StudentEditWindow(null) { Owner = Window.GetWindow(this) };
            bool result = window.ShowDialog() == true;

            if (result)
            {
                await LoadStudents(null);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Student student = (Student)button.Tag;

            StudentEditWindow window = new StudentEditWindow(student) { Owner = Window.GetWindow(this) };
            bool result = window.ShowDialog() == true;

            if (result)
            {
                await LoadStudents(null);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Student student = (Student)button.Tag;

            MessageBoxResult confirm = MessageBox.Show(
                student.Name + " 교육생을 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await ApiClient.DeleteStudent(student.Id);
            }
            catch
            {
                MessageBox.Show("삭제에 실패했습니다.", "오류");
                return;
            }

            await LoadStudents(null);
        }

        private async void FaceRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Student student = (Student)button.Tag;

            FaceRegisterWindow window = new FaceRegisterWindow(student) { Owner = Window.GetWindow(this) };
            window.ShowDialog();

            await LoadStudents(null);
        }
    }
}
