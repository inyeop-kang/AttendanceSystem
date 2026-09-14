using System;

namespace AttendanceClient.Services
{
    // 서버 접속 정보
    public static class Config
    {
        // "localhost"는 IPv6(::1)로 먼저 해석되는데 서버는 IPv4(0.0.0.0)로만 리스닝하므로,
        // ::1 접속 시도가 약 2초 타임아웃된 뒤에야 IPv4로 폴백된다. 그래서 IP를 직접 지정한다.
        public static readonly string ServerHost = GetStringEnv("ATTENDANCE_SERVER_HOST", "127.0.0.1");
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
