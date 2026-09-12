using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AttendanceServer.Data;
using AttendanceServer.Services;

namespace AttendanceServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Db db = new Db(Config.DbConnectionString);
            StudentRepository studentRepository = new StudentRepository(db);
            FaceEmbeddingRepository faceEmbeddingRepository = new FaceEmbeddingRepository(db);
            AttendanceRepository attendanceRepository = new AttendanceRepository(db);
            AdminRepository adminRepository = new AdminRepository(db);
            AiServerClient aiServerClient = new AiServerClient(Config.AiServerHost, Config.AiServerPort);

            SeedAdmin(adminRepository);

            RequestHandler requestHandler = new RequestHandler(
                studentRepository,
                faceEmbeddingRepository,
                attendanceRepository,
                adminRepository,
                aiServerClient);

            TcpListener listener = new TcpListener(IPAddress.Any, Config.ServerPort);
            listener.Start();
            Console.WriteLine("C# 메인 서버 시작: 0.0.0.0:" + Config.ServerPort);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Task.Run(() => HandleClient(client, requestHandler));
            }
        }

        private static async Task HandleClient(TcpClient client, RequestHandler requestHandler)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    while (true)
                    {
                        string requestJson = await SocketProtocol.ReadMessage(stream);

                        if (requestJson == null)
                        {
                            break;
                        }

                        string responseJson = await requestHandler.Handle(requestJson);
                        await SocketProtocol.WriteMessage(stream, responseJson);
                    }
                }
            }
            catch
            {
                // 연결이 끊기거나 잘못된 요청이 오면 그냥 연결을 종료한다.
            }
        }

        private static void SeedAdmin(AdminRepository adminRepository)
        {
            int count = adminRepository.Count();

            if (count == 0)
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword("admin1234");
                adminRepository.Insert("admin", passwordHash);
            }
        }
    }
}
