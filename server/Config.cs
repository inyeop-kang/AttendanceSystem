using System;

namespace AttendanceServer
{
    public static class Config
    {
        public static int ServerPort = GetIntEnv("ATTENDANCE_SERVER_PORT", 5080);

        public static string DbConnectionString = GetStringEnv(
            "ATTENDANCE_DB_CONNECTION",
            "Server=localhost;Port=3306;Database=attendance_system;User=attendance_app;Password=AppUser!2026;");

        public static string AiServerHost = GetStringEnv("ATTENDANCE_AI_HOST", "localhost");
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
