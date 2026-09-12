using System;
using System.Collections.Generic;
using MySqlConnector;
using AttendanceServer.Models;

namespace AttendanceServer.Data
{
    public class AttendanceRepository
    {
        private readonly Db db;

        public AttendanceRepository(Db db)
        {
            this.db = db;
        }

        private string GetTimeText(MySqlDataReader reader, string column)
        {
            DateTime value = Db.GetDateTimeOrMin(reader, column);

            if (value == DateTime.MinValue)
            {
                return "";
            }

            return value.ToString("HH:mm:ss");
        }

        public AttendanceRecord GetByStudentAndDate(int studentId, DateTime date)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT id, student_id, attendance_date, check_in_time, check_out_time, status, confidence " +
                             "FROM attendance WHERE student_id = @studentId AND attendance_date = @date";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@date", date.Date);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        AttendanceRecord record = new AttendanceRecord();
                        record.Id = reader.GetInt32("id");
                        record.StudentId = reader.GetInt32("student_id");
                        record.AttendanceDate = reader.GetDateTime("attendance_date");
                        record.Status = reader.GetString("status");
                        record.CheckInTime = Db.GetDateTimeOrMin(reader, "check_in_time");
                        record.CheckOutTime = Db.GetDateTimeOrMin(reader, "check_out_time");
                        record.Confidence = Db.GetDoubleOrZero(reader, "confidence");
                        return record;
                    }
                }
            }

            return null;
        }

        public void CreateCheckIn(int studentId, DateTime checkInTime, string status, double confidence)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "INSERT INTO attendance (student_id, attendance_date, check_in_time, status, confidence) " +
                             "VALUES (@studentId, @date, @checkInTime, @status, @confidence)";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@date", checkInTime.Date);
                command.Parameters.AddWithValue("@checkInTime", checkInTime);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@confidence", confidence);
                command.ExecuteNonQuery();
            }
        }

        public bool SetCheckOut(int studentId, DateTime date, DateTime checkOutTime)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "UPDATE attendance SET check_out_time = @checkOutTime " +
                             "WHERE student_id = @studentId AND attendance_date = @date";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@checkOutTime", checkOutTime);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@date", date.Date);

                int affected = command.ExecuteNonQuery();
                return affected > 0;
            }
        }

        public List<TodayAttendanceItem> GetListByDateRange(DateTime startDate, DateTime endDate)
        {
            List<TodayAttendanceItem> list = new List<TodayAttendanceItem>();

            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT s.id, s.student_no, s.name, s.department, s.grade, " +
                             "a.attendance_date, a.status, a.check_in_time, a.check_out_time " +
                             "FROM students s " +
                             "LEFT JOIN attendance a ON a.student_id = s.id " +
                             "AND a.attendance_date BETWEEN @startDate AND @endDate " +
                             "ORDER BY s.name, a.attendance_date";

                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@startDate", startDate.Date);
                command.Parameters.AddWithValue("@endDate", endDate.Date);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TodayAttendanceItem item = new TodayAttendanceItem();
                        item.StudentId = reader.GetInt32("id");
                        item.StudentNo = reader.GetString("student_no");
                        item.Name = reader.GetString("name");
                        item.Department = Db.GetStringOrEmpty(reader, "department");
                        item.Grade = Db.GetIntOrZero(reader, "grade");

                        if (reader.IsDBNull(reader.GetOrdinal("status")))
                        {
                            item.Date = "";
                            item.Status = "absent";
                            item.CheckInTime = "";
                            item.CheckOutTime = "";
                        }
                        else
                        {
                            item.Date = reader.GetDateTime("attendance_date").ToString("yyyy-MM-dd");
                            item.Status = reader.GetString("status");
                            item.CheckInTime = GetTimeText(reader, "check_in_time");
                            item.CheckOutTime = GetTimeText(reader, "check_out_time");
                        }

                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
