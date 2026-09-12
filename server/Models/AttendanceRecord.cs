using System;

namespace AttendanceServer.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime CheckOutTime { get; set; }
        public string Status { get; set; }
        public double Confidence { get; set; }
    }
}
