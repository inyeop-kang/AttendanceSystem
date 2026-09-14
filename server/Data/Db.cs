using System;
using System.Diagnostics;
using MySqlConnector;

namespace AttendanceServer.Data
{
    public class Db
    {
        private readonly string connectionString;

        public Db(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public MySqlConnection OpenConnection()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();
            stopwatch.Stop();
            Console.WriteLine("[timing]   db.OpenConnection " + stopwatch.ElapsedMilliseconds + "ms");
            return connection;
        }

        // DB에서 값이 NULL일 수 있는 컬럼을 안전하게 읽어오는 도우미 메서드들.
        // NULL이면 빈 문자열/0/DateTime.MinValue로 대신 채워준다.

        public static string GetStringOrEmpty(MySqlDataReader reader, string column)
        {
            if (reader.IsDBNull(reader.GetOrdinal(column)))
            {
                return "";
            }

            return reader.GetString(column);
        }

        public static int GetIntOrZero(MySqlDataReader reader, string column)
        {
            if (reader.IsDBNull(reader.GetOrdinal(column)))
            {
                return 0;
            }

            return reader.GetInt32(column);
        }

        public static DateTime GetDateTimeOrMin(MySqlDataReader reader, string column)
        {
            if (reader.IsDBNull(reader.GetOrdinal(column)))
            {
                return DateTime.MinValue;
            }

            return reader.GetDateTime(column);
        }

        public static double GetDoubleOrZero(MySqlDataReader reader, string column)
        {
            if (reader.IsDBNull(reader.GetOrdinal(column)))
            {
                return 0.0;
            }

            return reader.GetDouble(column);
        }
    }
}
