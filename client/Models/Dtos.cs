using System;
using System.Collections.Generic;

namespace AttendanceClient.Models
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

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AdminUsername { get; set; }
    }

    public class StudentCreateRequest
    {
        public string StudentNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int Grade { get; set; }
    }

    public class StudentCreateResult
    {
        public string Error { get; set; }
        public Student Student { get; set; }
    }

    public class SimpleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class FaceRegisterRequest
    {
        public List<string> Images { get; set; }
    }

    public class FaceRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ValidFrameCount { get; set; }
        public int TotalFrameCount { get; set; }
    }

    public class AttendanceCheckRequest
    {
        public List<string> Images { get; set; }
    }

    public class AttendanceCheckResponse
    {
        public bool Matched { get; set; }
        public string Message { get; set; }
        public int StudentId { get; set; }
        public string StudentNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int Grade { get; set; }
        public double Confidence { get; set; }
        public string Status { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime CheckOutTime { get; set; }
    }

    public class TodayAttendanceItem
    {
        public int StudentId { get; set; }
        public string StudentNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int Grade { get; set; }
        public string Date { get; set; }
        public string Status { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
    }

    public class TodaySummary
    {
        public int TotalCount { get; set; }
        public int CheckedInCount { get; set; }
        public int CheckedOutCount { get; set; }
        public int AbsentCount { get; set; }
        public List<TodayAttendanceItem> Items { get; set; }
    }
}
