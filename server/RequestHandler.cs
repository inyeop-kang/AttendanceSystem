using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AttendanceServer.Data;
using AttendanceServer.Models;
using AttendanceServer.Services;

namespace AttendanceServer
{
    // 클라이언트로부터 온 action 기반 요청을 처리해서 응답 JSON 문자열을 만든다.
    // 예전 ASP.NET Core Controller들의 역할을 그대로 대체한다.
    public class RequestHandler
    {
        private readonly StudentRepository studentRepository;
        private readonly FaceEmbeddingRepository faceEmbeddingRepository;
        private readonly AttendanceRepository attendanceRepository;
        private readonly AdminRepository adminRepository;
        private readonly AiServerClient aiServerClient;

        private const int MinimumValidFrames = 3;

        public RequestHandler(
            StudentRepository studentRepository,
            FaceEmbeddingRepository faceEmbeddingRepository,
            AttendanceRepository attendanceRepository,
            AdminRepository adminRepository,
            AiServerClient aiServerClient)
        {
            this.studentRepository = studentRepository;
            this.faceEmbeddingRepository = faceEmbeddingRepository;
            this.attendanceRepository = attendanceRepository;
            this.adminRepository = adminRepository;
            this.aiServerClient = aiServerClient;
        }

        public async Task<string> Handle(string requestJson)
        {
            object response;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(requestJson))
                {
                    JsonElement root = doc.RootElement;
                    string action = GetString(root, "action");

                    if (action == "login")
                    {
                        response = HandleLogin(root);
                    }
                    else if (action == "getStudents")
                    {
                        response = HandleGetStudents(root);
                    }
                    else if (action == "createStudent")
                    {
                        response = HandleCreateStudent(root);
                    }
                    else if (action == "updateStudent")
                    {
                        response = HandleUpdateStudent(root);
                    }
                    else if (action == "deleteStudent")
                    {
                        response = HandleDeleteStudent(root);
                    }
                    else if (action == "registerFace")
                    {
                        response = await HandleRegisterFace(root);
                    }
                    else if (action == "checkIn")
                    {
                        response = await HandleCheckIn(root);
                    }
                    else if (action == "checkOut")
                    {
                        response = await HandleCheckOut(root);
                    }
                    else if (action == "getAttendanceSummary")
                    {
                        response = HandleGetAttendanceSummary(root);
                    }
                    else
                    {
                        StudentCreateResult errorResult = new StudentCreateResult();
                        errorResult.Error = "알 수 없는 action: " + action;
                        response = errorResult;
                    }
                }
            }
            catch (Exception e)
            {
                StudentCreateResult errorResult = new StudentCreateResult();
                errorResult.Error = "서버 오류: " + e.Message;
                response = errorResult;
            }

            return JsonSerializer.Serialize(response);
        }

        // ------------------------------------------------------------
        // JsonElement에서 값을 안전하게 꺼내는 도우미 메서드들
        // ------------------------------------------------------------

        private static string GetString(JsonElement root, string name)
        {
            JsonElement value;
            bool found = root.TryGetProperty(name, out value);

            if (!found || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return value.GetString();
        }

        private static int GetInt(JsonElement root, string name)
        {
            JsonElement value;
            bool found = root.TryGetProperty(name, out value);

            if (!found || value.ValueKind == JsonValueKind.Null)
            {
                return 0;
            }

            return value.GetInt32();
        }

        private static List<string> GetStringList(JsonElement root, string name)
        {
            List<string> list = new List<string>();
            JsonElement value;
            bool found = root.TryGetProperty(name, out value);

            if (!found)
            {
                return list;
            }

            foreach (JsonElement item in value.EnumerateArray())
            {
                list.Add(item.GetString());
            }

            return list;
        }

        // ------------------------------------------------------------
        // 로그인
        // ------------------------------------------------------------

        private LoginResponse HandleLogin(JsonElement root)
        {
            string username = GetString(root, "username");
            string password = GetString(root, "password");

            LoginResponse response = new LoginResponse();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                response.Success = false;
                response.Message = "아이디와 비밀번호를 입력해 주세요.";
                return response;
            }

            string passwordHash = adminRepository.GetPasswordHash(username);

            if (passwordHash == null)
            {
                response.Success = false;
                response.Message = "아이디 또는 비밀번호가 올바르지 않습니다.";
                return response;
            }

            bool valid = BCrypt.Net.BCrypt.Verify(password, passwordHash);

            if (!valid)
            {
                response.Success = false;
                response.Message = "아이디 또는 비밀번호가 올바르지 않습니다.";
                return response;
            }

            response.Success = true;
            response.Message = "로그인 성공";
            response.AdminUsername = username;
            return response;
        }

        // ------------------------------------------------------------
        // 교육생 관리
        // ------------------------------------------------------------

        private List<Student> HandleGetStudents(JsonElement root)
        {
            string keyword = GetString(root, "keyword");
            string department = GetString(root, "department");
            return studentRepository.Search(keyword, department);
        }

        private StudentCreateResult HandleCreateStudent(JsonElement root)
        {
            StudentCreateRequest request = new StudentCreateRequest();
            request.StudentNo = GetString(root, "studentNo");
            request.Name = GetString(root, "name");
            request.Department = GetString(root, "department");
            request.Grade = GetInt(root, "grade");

            StudentCreateResult result = new StudentCreateResult();

            if (string.IsNullOrEmpty(request.StudentNo) || string.IsNullOrEmpty(request.Name))
            {
                result.Error = "교육생 번호와 이름은 필수입니다.";
                return result;
            }

            Student existing = studentRepository.GetByStudentNo(request.StudentNo);

            if (existing != null)
            {
                result.Error = "이미 등록된 교육생 번호입니다.";
                return result;
            }

            int newId = studentRepository.Insert(request);
            result.Student = studentRepository.GetById(newId);
            return result;
        }

        private object HandleUpdateStudent(JsonElement root)
        {
            int id = GetInt(root, "id");

            Student existing = studentRepository.GetById(id);

            if (existing == null)
            {
                return new SimpleResult(false, "교육생을 찾을 수 없습니다.");
            }

            StudentUpdateRequest request = new StudentUpdateRequest();
            request.StudentNo = GetString(root, "studentNo");
            request.Name = GetString(root, "name");
            request.Department = GetString(root, "department");
            request.Grade = GetInt(root, "grade");

            studentRepository.Update(id, request);
            return new SimpleResult(true, "");
        }

        private object HandleDeleteStudent(JsonElement root)
        {
            int id = GetInt(root, "id");

            Student existing = studentRepository.GetById(id);

            if (existing == null)
            {
                return new SimpleResult(false, "교육생을 찾을 수 없습니다.");
            }

            studentRepository.Delete(id);
            return new SimpleResult(true, "");
        }

        private async Task<FaceRegisterResponse> HandleRegisterFace(JsonElement root)
        {
            int studentId = GetInt(root, "studentId");
            List<string> images = GetStringList(root, "images");

            FaceRegisterResponse response = new FaceRegisterResponse();

            Student student = studentRepository.GetById(studentId);

            if (student == null)
            {
                response.Success = false;
                response.Message = "교육생을 찾을 수 없습니다.";
                return response;
            }

            if (images.Count == 0)
            {
                response.Success = false;
                response.Message = "촬영된 이미지가 없습니다.";
                return response;
            }

            ExtractEmbeddingsResult result = await aiServerClient.ExtractEmbeddings(images);

            response.TotalFrameCount = result.TotalCount;
            response.ValidFrameCount = result.ValidCount;

            if (result.ValidCount < MinimumValidFrames)
            {
                response.Success = false;
                response.Message = "얼굴 인식이 가능한 프레임이 부족합니다. 조명을 밝게 하고 다시 촬영해 주세요.";
                return response;
            }

            faceEmbeddingRepository.DeleteByStudentId(studentId);

            foreach (List<double> embedding in result.Embeddings)
            {
                string embeddingJson = JsonSerializer.Serialize(embedding);
                faceEmbeddingRepository.Insert(studentId, embeddingJson, result.ModelName);
            }

            studentRepository.MarkFaceRegistered(studentId);

            response.Success = true;
            response.Message = "얼굴 등록이 완료되었습니다.";
            return response;
        }

        // ------------------------------------------------------------
        // 출석 처리
        // ------------------------------------------------------------

        // 이미지로 얼굴을 인식해서 학생을 찾는다. 실패하면 response를 채우고 null을 반환한다.
        private async Task<Student> RecognizeStudent(List<string> images, AttendanceCheckResponse response)
        {
            if (images.Count == 0)
            {
                response.Matched = false;
                response.Message = "이미지가 없습니다.";
                return null;
            }

            RecognizeResult result = await aiServerClient.Recognize(images);
            response.Confidence = result.Similarity;

            if (!result.Matched)
            {
                response.Matched = false;
                response.Message = "등록되지 않은 사용자입니다. (인식 실패)";
                return null;
            }

            Student student = studentRepository.GetById(result.StudentId);

            if (student == null)
            {
                response.Matched = false;
                response.Message = "등록되지 않은 사용자입니다. (인식 실패)";
                return null;
            }

            response.Matched = true;
            response.StudentId = student.Id;
            response.StudentNo = student.StudentNo;
            response.Name = student.Name;
            response.Department = student.Department;
            response.Grade = student.Grade;
            return student;
        }

        private async Task<AttendanceCheckResponse> HandleCheckIn(JsonElement root)
        {
            List<string> images = GetStringList(root, "images");
            AttendanceCheckResponse response = new AttendanceCheckResponse();
            Student student = await RecognizeStudent(images, response);

            if (student == null)
            {
                return response;
            }

            DateTime now = DateTime.Now;
            AttendanceRecord existing = attendanceRepository.GetByStudentAndDate(student.Id, now);

            if (existing != null)
            {
                response.Status = existing.CheckOutTime == DateTime.MinValue ? "checked_in" : "checked_out";
                response.CheckInTime = existing.CheckInTime;
                response.CheckOutTime = existing.CheckOutTime;
                response.Message = student.Name + "님은 오늘 이미 입실 처리되었습니다.";
                return response;
            }

            attendanceRepository.CreateCheckIn(student.Id, now, "present", response.Confidence);

            response.Status = "checked_in";
            response.CheckInTime = now;
            response.Message = student.Name + "님, 입실 처리되었습니다.";

            return response;
        }

        private async Task<AttendanceCheckResponse> HandleCheckOut(JsonElement root)
        {
            List<string> images = GetStringList(root, "images");
            AttendanceCheckResponse response = new AttendanceCheckResponse();
            Student student = await RecognizeStudent(images, response);

            if (student == null)
            {
                return response;
            }

            DateTime now = DateTime.Now;
            AttendanceRecord existing = attendanceRepository.GetByStudentAndDate(student.Id, now);

            if (existing == null)
            {
                response.Message = student.Name + "님, 먼저 입실을 해야 합니다.";
                return response;
            }

            response.CheckInTime = existing.CheckInTime;

            if (existing.CheckOutTime != DateTime.MinValue)
            {
                response.Status = "checked_out";
                response.CheckOutTime = existing.CheckOutTime;
                response.Message = student.Name + "님은 오늘 이미 퇴실 처리되었습니다.";
                return response;
            }

            attendanceRepository.SetCheckOut(student.Id, now, now);

            response.Status = "checked_out";
            response.CheckOutTime = now;
            response.Message = student.Name + "님, 퇴실 처리되었습니다.";
            return response;
        }

        private DateTime ParseDateOrToday(string text)
        {
            DateTime value;
            bool ok = DateTime.TryParse(text, out value);

            if (!ok)
            {
                value = DateTime.Now.Date;
            }

            return value;
        }

        private TodaySummary HandleGetAttendanceSummary(JsonElement root)
        {
            DateTime rangeStart = ParseDateOrToday(GetString(root, "startDate"));
            DateTime rangeEnd = ParseDateOrToday(GetString(root, "endDate"));

            List<TodayAttendanceItem> items = attendanceRepository.GetListByDateRange(rangeStart, rangeEnd);

            TodaySummary summary = new TodaySummary();
            summary.Items = items;
            summary.TotalCount = items.Count;
            summary.CheckedInCount = 0;
            summary.CheckedOutCount = 0;
            summary.AbsentCount = 0;

            foreach (TodayAttendanceItem item in items)
            {
                if (item.Status == "checked_in")
                {
                    summary.CheckedInCount = summary.CheckedInCount + 1;
                }
                else if (item.Status == "checked_out")
                {
                    summary.CheckedOutCount = summary.CheckedOutCount + 1;
                }
                else
                {
                    summary.AbsentCount = summary.AbsentCount + 1;
                }
            }

            return summary;
        }
    }

    public class SimpleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public SimpleResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
