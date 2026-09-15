using System;
using System.Collections.Generic;

namespace AttendanceServer.Models
{
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

    public class StudentUpdateRequest
    {
        public string StudentNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int Grade { get; set; }
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
        // 인식 실패 시 사유 코드("spoof_suspected"/"no_match"). 클라이언트가 이 값으로
        // 재생할 음성(mp3)을 고른다 — Message는 화면 표시용 문구라 음성 문구와 다를 수 있음.
        public string SoundKey { get; set; }
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
        // 입실했지만 아직 퇴실하지 않은 인원
        public int CheckedInCount { get; set; }
        // 퇴실까지 마친 인원
        public int CheckedOutCount { get; set; }
        public int AbsentCount { get; set; }
        public List<TodayAttendanceItem> Items { get; set; }
    }

    // 교육생 1명의 기간별 통계에서 하루치 기록을 나타낸다.
    public class StudentStatsItem
    {
        public string Date { get; set; }
        public string Status { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        // 입실~퇴실 사이 시간. 퇴실 기록이 없으면 빈 문자열.
        public string StayDuration { get; set; }
    }

    public class StudentStatsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int StudentId { get; set; }
        public string StudentNo { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int Grade { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        // 기간 안에서 출결 기록이 하나라도 있는 날짜의 수(= 교육이 진행된 날).
        // 주말·공휴일 정보를 따로 관리하지 않으므로 이 값을 분모로 사용한다.
        public int OperatingDayCount { get; set; }
        public int PresentDayCount { get; set; }
        public int AbsentDayCount { get; set; }
        // 출석률(%). 운영일이 0이면 0.
        public double AttendanceRate { get; set; }
        public string AverageCheckInTime { get; set; }
        public string AverageCheckOutTime { get; set; }
        public string AverageStayDuration { get; set; }
        public string TotalStayDuration { get; set; }
        // 입실만 하고 퇴실 기록이 없는 날의 수
        public int MissingCheckOutCount { get; set; }
        public List<StudentStatsItem> Items { get; set; }
    }
}
