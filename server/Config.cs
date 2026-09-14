using System;

namespace AttendanceServer
{
    public static class Config
    {
        public static int ServerPort = GetIntEnv("ATTENDANCE_SERVER_PORT", 5080);

        public static string DbConnectionString = GetStringEnv(
            "ATTENDANCE_DB_CONNECTION",
            "Server=127.0.0.1;Port=3306;Database=attendance_system;User=attendance_app;Password=AppUser!2026;");

        // AI 서버는 IPv4(0.0.0.0)로만 리스닝하므로 "localhost"를 쓰면 IPv6(::1) 접속이
        // 약 2초 타임아웃된 뒤에야 IPv4로 폴백된다. 그래서 IP를 직접 지정한다.
        public static string AiServerHost = GetStringEnv("ATTENDANCE_AI_HOST", "127.0.0.1");
        public static int AiServerPort = GetIntEnv("ATTENDANCE_AI_PORT", 8001);

        public static TimeSpan LateCutoffTime = new TimeSpan(9, 0, 0);

        private static string GetStringEnv(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return value;
        }

        private static int GetIntEnv(string name, int defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            int parsed;
            bool ok = int.TryParse(value, out parsed);

            if (!ok)
            {
                return defaultValue;
            }

            return parsed;
        }
    }
}
