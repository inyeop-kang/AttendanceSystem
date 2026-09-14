using System;
using System.IO;
using System.Windows.Media;

namespace AttendanceClient.Services
{
    // 입실/퇴실 효과음. WPF의 SoundPlayer는 mp3를 못 틀어서(wav 전용),
    // System.Windows.Media.MediaPlayer(Windows Media Foundation 기반)를 쓴다.
    public static class Sound
    {
        private static readonly string AssetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");

        // MediaPlayer 인스턴스를 함수 안에서 지역변수로 만들면 재생 중 GC에 회수될 수 있어서
        // 재생이 끝날 때까지 살려둘 참조를 static 필드에 보관한다.
        private static MediaPlayer checkInPlayer;
        private static MediaPlayer checkOutPlayer;

        public static void PlayCheckIn()
        {
            Play(ref checkInPlayer, "check_in.mp3");
        }

        public static void PlayCheckOut()
        {
            Play(ref checkOutPlayer, "check_out.mp3");
        }

        private static void Play(ref MediaPlayer player, string fileName)
        {
            try
            {
                string path = Path.Combine(AssetsDir, fileName);
                if (!File.Exists(path))
                {
                    return;
                }

                player = new MediaPlayer();
                player.Open(new Uri(path, UriKind.Absolute));
                player.Play();
            }
            catch
            {
                // 효과음은 부가 기능이라, 재생 실패(오디오 장치 없음 등)가 출석 처리
                // 자체를 막으면 안 된다. 조용히 무시한다.
            }
        }
    }
}
