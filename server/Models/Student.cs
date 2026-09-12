using System;

namespace AttendanceServer.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string StudentNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int Grade { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool HasFaceRegistered { get; set; }
    }
}
