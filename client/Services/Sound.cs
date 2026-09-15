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
        private static MediaPlayer checkInVoicePlayer;
        private static MediaPlayer checkOutVoicePlayer;
        private static MediaPlayer spoofSuspectedPlayer;
        private static MediaPlayer noMatchPlayer;
        private static MediaPlayer connectionErrorPlayer;
        private static MediaPlayer sendFailurePlayer;
        private static MediaPlayer saveFailurePlayer;
        private static MediaPlayer commFailurePlayer;

        public static void PlayCheckIn()
        {
            Play(ref checkInPlayer, "check_in.mp3");
        }

        public static void PlayCheckOut()
        {
            Play(ref checkOutPlayer, "check_out.mp3");
        }

        // 입실/퇴실 성공 시 "입실하셨습니다"/"퇴실하셨습니다" 음성 안내.
        // check_in.mp3/check_out.mp3(효과음)와 별개로 같이 재생된다.
        // 이름을 불러주는 기능은 매번 텍스트가 달라져 실시간 API가 필요하므로 별도 계획.
        public static void PlayCheckInVoice()
        {
            Play(ref checkInVoicePlayer, "checkin_voice.mp3");
        }

        public static void PlayCheckOutVoice()
        {
            Play(ref checkOutVoicePlayer, "checkout_voice.mp3");
        }

        // 인식 실패 사유별 음성 안내. 문구/화자는 docs/plan-tts-voice-guidance.md 참고.
        // mp3 파일은 아직 생성 전이라 Play()가 조용히 무시한다 (파일 없으면 재생 안 함).
        public static void PlaySpoofSuspected()
        {
            Play(ref spoofSuspectedPlayer, "spoof_suspected.mp3");
        }

        public static void PlayNoMatch()
        {
            Play(ref noMatchPlayer, "no_match.mp3");
        }

        public static void PlayConnectionError()
        {
            Play(ref connectionErrorPlayer, "connection_error.mp3");
        }

        public static void PlaySendFailure()
        {
            Play(ref sendFailurePlayer, "send_failure.mp3");
        }

        public static void PlaySaveFailure()
        {
            Play(ref saveFailurePlayer, "save_failure.mp3");
        }

        public static void PlayCommFailure()
        {
            Play(ref commFailurePlayer, "comm_failure.mp3");
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
