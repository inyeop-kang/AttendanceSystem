using System.Windows;
using AttendanceClient.Models;
using AttendanceClient.Services;

namespace AttendanceClient.Views
{
    public partial class StudentEditWindow : Window
    {
        private readonly Student editingStudent;

        public StudentEditWindow(Student student)
        {
            InitializeComponent();
            editingStudent = student;

            if (editingStudent != null)
            {
                Title = "교육생 정보 수정";
                StudentNoBox.Text = editingStudent.StudentNo;
                NameBox.Text = editingStudent.Name;
                DepartmentBox.Text = editingStudent.Department;
                GradeBox.Text = editingStudent.Grade.ToString();
            }
            else
            {
                Title = "교육생 신규 등록";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string studentNo = StudentNoBox.Text;
            string name = NameBox.Text;
            string department = DepartmentBox.Text;
            string gradeText = GradeBox.Text;

            MessageText.Text = "";

            if (string.IsNullOrEmpty(studentNo) || string.IsNullOrEmpty(name))
            {
                MessageText.Text = "교육생 번호와 이름은 필수입니다.";
                return;
            }

            int grade = 0;
            if (!string.IsNullOrEmpty(gradeText))
            {
                bool parsed = int.TryParse(gradeText, out grade);
                if (!parsed)
                {
                    MessageText.Text = "기수는 숫자로 입력해 주세요.";
                    return;
                }
            }

            bool isNewStudent = editingStudent == null;
            Student savedStudent = null;

            try
            {
                if (isNewStudent)
                {
                    savedStudent = await ApiClient.CreateStudent(studentNo, name, department, grade);
                }
                else
                {
                    await ApiClient.UpdateStudent(editingStudent.Id, studentNo, name, department, grade);
                }
            }
            catch
            {
                MessageText.Text = "저장에 실패했습니다. (교육생 번호 중복 또는 서버 오류)";
                Sound.PlaySaveFailure();
                MessageBox.Show(MessageText.Text, "오류");
                return;
            }

            if (isNewStudent && savedStudent != null)
            {
                MessageBoxResult goRegisterFace = MessageBox.Show(
                    "교육생 등록이 완료되었습니다. 지금 바로 얼굴을 등록하시겠습니까?",
                    "얼굴 등록", MessageBoxButton.YesNo);

                if (goRegisterFace == MessageBoxResult.Yes)
                {
                    FaceRegisterWindow faceWindow = new FaceRegisterWindow(savedStudent);
                    faceWindow.Owner = this;
                    faceWindow.ShowDialog();
                }
            }

            DialogResult = true;
            Close();
        }
    }
}
