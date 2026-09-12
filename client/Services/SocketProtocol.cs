using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceClient.Services
{
    // 메시지 형식: [4바이트 길이(빅엔디안)][그 길이만큼의 UTF-8 텍스트(JSON)]
    public static class SocketProtocol
    {
        public static async Task<string> ReadMessage(NetworkStream stream)
        {
            byte[] lengthBytes = await ReadExact(stream, 4);

            if (lengthBytes == null)
            {
                return null;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            int length = BitConverter.ToInt32(lengthBytes, 0);

            byte[] bodyBytes = await ReadExact(stream, length);

            if (bodyBytes == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bodyBytes);
        }

        public static async Task WriteMessage(NetworkStream stream, string text)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(text);
            byte[] lengthBytes = BitConverter.GetBytes(bodyBytes.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
        }

        private static async Task<byte[]> ReadExact(NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            int offset = 0;

            while (offset < size)
            {
                int read = await stream.ReadAsync(buffer, offset, size - offset);

                if (read == 0)
                {
                    return null;
                }

                offset = offset + read;
            }

            return buffer;
        }
    }
}
