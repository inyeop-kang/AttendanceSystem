using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AttendanceServer.Services
{
    public class ExtractEmbeddingsResult
    {
        public List<List<double>> Embeddings { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("valid_count")]
        public int ValidCount { get; set; }

        [JsonPropertyName("model_name")]
        public string ModelName { get; set; }
    }

    public class RecognizeResult
    {
        public bool Matched { get; set; }

        [JsonPropertyName("student_id")]
        public int StudentId { get; set; }

        public double Similarity { get; set; }

        // "spoof_suspected" 등 매칭 실패 사유. 없으면 null.
        public string Reason { get; set; }
    }

    public class AiServerClient
    {
        private readonly string host;
        private readonly int port;
        private static readonly JsonSerializerOptions jsonOptions = CreateJsonOptions();

        public AiServerClient(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            return options;
        }

        // AI 서버에 TCP 소켓으로 연결해서 action 요청을 보내고, 응답 JSON 문자열을 그대로 받아온다.
        private async Task<string> SendRequest(Dictionary<string, object> request)
        {
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(host, port);

                using (NetworkStream stream = client.GetStream())
                {
                    string requestJson = JsonSerializer.Serialize(request);
                    await SocketProtocol.WriteMessage(stream, requestJson);
                    return await SocketProtocol.ReadMessage(stream);
                }
            }
        }

        public async Task<ExtractEmbeddingsResult> ExtractEmbeddings(List<string> images)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request.Add("action", "extract_embeddings");
            request.Add("images", images);

            string responseText = await SendRequest(request);
            return JsonSerializer.Deserialize<ExtractEmbeddingsResult>(responseText, jsonOptions);
        }

        public async Task<RecognizeResult> Recognize(List<string> images)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            request.Add("action", "recognize");
            request.Add("images", images);

            string responseText = await SendRequest(request);
            return JsonSerializer.Deserialize<RecognizeResult>(responseText, jsonOptions);
        }
    }
}
