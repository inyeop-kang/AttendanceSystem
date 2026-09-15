using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AttendanceClient.Models;

namespace AttendanceClient.Services
{
    public class ApiClient
    {
        private static readonly JsonSerializerOptions jsonOptions = CreateJsonOptions();

        private static JsonSerializerOptions CreateJsonOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            return options;
        }

        // 서버에 action 기반 JSON 요청을 보내고, 응답 JSON을 원하는 타입으로 변환해서 돌려준다.
        private static async Task<T> Send<T>(Dictionary<string, object> request)
        {
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(Config.ServerHost, Config.ServerPort);

                using (NetworkStream stream = client.GetStream())
                {
                    string requestJson = JsonSerializer.Serialize(request);
                    await SocketProtocol.WriteMessage(stream, requestJson);

                    string responseJson = await SocketProtocol.ReadMessage(stream);
                    return JsonSerializer.Deserialize<T>(responseJson, jsonOptions);
                }
            }
        }

        public static async Task<LoginResponse> Login(string username, string password)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "login";
            request["username"] = username;
            request["password"] = password;
            return await Send<LoginResponse>(request);
        }

        public static async Task<List<Student>> GetStudents(string keyword, string department)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "getStudents";
            request["keyword"] = keyword;
            request["department"] = department;
            return await Send<List<Student>>(request);
        }

        public static async Task<Student> CreateStudent(string studentNo, string name, string department, int grade)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "createStudent";
            request["studentNo"] = studentNo;
            request["name"] = name;
            request["department"] = department;
            request["grade"] = grade;

            StudentCreateResult result = await Send<StudentCreateResult>(request);

            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }

            return result.Student;
        }

        public static async Task UpdateStudent(int id, string studentNo, string name, string department, int grade)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "updateStudent";
            request["id"] = id;
            request["studentNo"] = studentNo;
            request["name"] = name;
            request["department"] = department;
            request["grade"] = grade;

            SimpleResult result = await Send<SimpleResult>(request);

            if (!result.Success)
            {
                throw new Exception(result.Message);
            }
        }

        public static async Task DeleteStudent(int id)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "deleteStudent";
            request["id"] = id;

            SimpleResult result = await Send<SimpleResult>(request);

            if (!result.Success)
            {
                throw new Exception(result.Message);
            }
        }

        public static async Task<FaceRegisterResponse> RegisterFace(int studentId, List<string> imagesBase64)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "registerFace";
            request["studentId"] = studentId;
            request["images"] = imagesBase64;
            return await Send<FaceRegisterResponse>(request);
        }

        public static async Task<AttendanceCheckResponse> CheckIn(List<string> imagesBase64)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "checkIn";
            request["images"] = imagesBase64;
            return await Send<AttendanceCheckResponse>(request);
        }

        public static async Task<AttendanceCheckResponse> CheckOut(List<string> imagesBase64)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "checkOut";
            request["images"] = imagesBase64;
            return await Send<AttendanceCheckResponse>(request);
        }

        public static async Task<TodaySummary> GetAttendanceSummary(DateTime startDate, DateTime endDate)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "getAttendanceSummary";
            request["startDate"] = startDate.ToString("yyyy-MM-dd");
            request["endDate"] = endDate.ToString("yyyy-MM-dd");
            return await Send<TodaySummary>(request);
        }

        public static async Task<StudentStatsResponse> GetStudentStats(int studentId, DateTime startDate, DateTime endDate)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request["action"] = "getStudentStats";
            request["studentId"] = studentId;
            request["startDate"] = startDate.ToString("yyyy-MM-dd");
            request["endDate"] = endDate.ToString("yyyy-MM-dd");
            return await Send<StudentStatsResponse>(request);
        }
    }
}
