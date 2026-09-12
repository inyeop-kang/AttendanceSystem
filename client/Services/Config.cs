using System;

namespace AttendanceClient.Services
{
    // 서버 접속 정보
    public static class Config
    {
        public static readonly string ServerHost = GetStringEnv("ATTENDANCE_SERVER_HOST", "localhost");
        public static readonly int ServerPort = GetIntEnv("ATTENDANCE_SERVER_PORT", 5080);

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
            int result;
            bool ok = int.TryParse(value, out result);

            if (!ok)
            {
                return defaultValue;
            }

            return result;
        }
    }
}
