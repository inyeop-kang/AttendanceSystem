# 벨소리·TTS 기능 정리 + DB 트랜잭션 개념 노트

이 문서는 `willro4540/AttendanceSystem-fork` 저장소(팀장 원본 `inyeop-kang/AttendanceSystem`을 기반으로
안티스푸핑·효과음·TTS 등을 추가한 우리 작업 저장소)에서 벨소리(효과음)/TTS 기능이 어떻게 추가됐는지와,
DB 트랜잭션 개념을 나중에 개발일지·완료보고서를 쓸 때 참고하기 위한 기술 요약이다.

---

## 1. 벨소리(효과음) — 입실/퇴실 성공 시

- **커밋**: `718622a` "입실/퇴실 체크 성공 시 효과음 추가"
- **왜**: 원본(팀장 코드)은 입실/퇴실이 성공해도 화면 텍스트만 바뀌고 소리가 전혀 없었음.
- **구현 방식**: WPF의 `System.Windows.Media.SoundPlayer`는 wav만 재생 가능하고 mp3를 못 틀기 때문에,
  Windows Media Foundation 기반인 `System.Windows.Media.MediaPlayer`를 대신 사용함
  (`client/Services/Sound.cs`).
- **관련 파일**:
  - `client/Services/Sound.cs` — 재생 로직. `MediaPlayer` 인스턴스를 지역변수가 아니라 static 필드에
    보관하는데, 지역변수로 두면 재생 중에 GC가 회수해버릴 수 있기 때문.
  - `client/Assets/Sounds/check_in.mp3`, `check_out.mp3` — 실제 효과음 파일.
  - `client/AttendanceClient.csproj` — `<Content Include="Assets\Sounds\*.mp3">`로 빌드 출력 폴더에
    자동 복사되도록 설정 (이 설정 덕분에 이후 mp3 파일을 폴더에 추가하기만 하면 별도 프로젝트 설정
    변경 없이 배포됨).
- **호출 지점**: `client/Views/AttendanceCheckView.xaml.cs`의 `ShowResult()`에서 입실 성공 시
  `Sound.PlayCheckIn()`, 퇴실 성공 시 `Sound.PlayCheckOut()` 호출.

## 2. TTS(문구 음성 안내)

- **배경**: 효과음 추가 이후에도 실패 상황(스푸핑 의심/미등록/서버 연결 실패 등)에는 여전히 아무
  소리도 없었음. 계획 문서: `docs/plan-tts-voice-guidance.md`.
- **핵심 결정 — 실시간 API 호출이 아니라 "미리 생성 후 로컬 재생"**:
  안내 문구가 몇 개로 고정되어 있고 매번 달라지지 않으므로(학생 이름을 부르는 등의 동적 문구가 아님),
  이벤트가 발생할 때마다 TTS API를 호출하는 대신 문구별로 딱 한 번만 Typecast API로 mp3를 만들어두고
  그 이후로는 벨소리와 같은 방식으로 로컬 파일만 재생함. 이유:
  - 실시간 API 호출은 인터넷 연결에 의존 → 네트워크가 새 장애 지점이 되고, 왕복 시간만큼 지연 발생.
    출결 키오스크처럼 즉시 반응해야 하는 화면에는 부적합.
  - 문구가 고정이므로 한 번만 생성하면 그 뒤로는 API 비용이 전혀 안 듦.
- **서버 → 클라이언트 사유 전달**: 인식 실패 사유(스푸핑 의심/미등록)는 서버(AI 서버 → C# 서버)만
  판단할 수 있으므로, 화면 표시용 문구(`Message`)와 별개로 `SoundKey`라는 필드를 새로 만들어
  전달함 (`server/Models/Dtos.cs`, `client/Models/Dtos.cs`에 동일하게 추가, `server/RequestHandler.cs`가
  값을 채움). `Message`는 화면에 보이는 긴 문구, `SoundKey`는 클라이언트가 어떤 mp3를 틀지 고르는
  코드값 — 둘을 분리한 이유는 음성 문구를 화면 문구보다 짧게 축약하기로 했기 때문(예: 화면엔
  "사진(정지 화면)으로 의심되어 인식이 거부되었습니다..."가 떠도, 음성은 "사진이 감지되었습니다"만
  짧게 재생).
- **클라이언트 재생 함수** (`client/Services/Sound.cs`, 총 8개, 벨소리 2개와 같은 패턴):

  | 함수 | 파일명 | 재생 문구 | 트리거 위치 |
  |---|---|---|---|
  | `PlayCheckInVoice` | `checkin_voice.mp3` | "입실하셨습니다" | 입실 성공 |
  | `PlayCheckOutVoice` | `checkout_voice.mp3` | "퇴실하셨습니다" | 퇴실 성공 |
  | `PlaySpoofSuspected` | `spoof_suspected.mp3` | "사진이 감지되었습니다" | 인식 실패(스푸핑 의심, 서버 `SoundKey` 응답) |
  | `PlayNoMatch` | `no_match.mp3` | "등록되지 않은 사용자입니다" | 인식 실패(그 외) |
  | `PlayConnectionError` | `connection_error.mp3` | "서버에 연결할 수 없습니다" | 로그인/관리자 재인증 화면 연결 예외 |
  | `PlaySendFailure` | `send_failure.mp3` | "전송에 실패했습니다" | 얼굴 등록 화면 전송 예외 |
  | `PlaySaveFailure` | `save_failure.mp3` | "저장에 실패했습니다" | 교육생 저장 화면 예외 |
  | `PlayCommFailure` | `comm_failure.mp3` | "통신에 실패했습니다" | 출석체크 화면 요청 자체 실패 |

- **mp3 생성 자동화**: `scripts/generate_tts_assets.ps1` — Typecast REST API
  (`POST https://api.typecast.ai/v1/text-to-speech`, model `ssfm-v30`)를 호출해서 위 8개 문구를
  한 번에 mp3로 만들어 `client/Assets/Sounds/`에 저장하는 스크립트. API 키(`TYPECAST_API_KEY`
  환경변수)와 목소리(`voice_id`)는 사용자가 직접 준비해서 실행해야 함 — 외부 서비스 계정이라
  대신 발급할 수 없는 부분.
- **현재 상태 (2026-09-15 기준)**: 코드 구조는 전부 완성, mp3 파일은 아직 생성 전(API 키 미발급).
  `Sound.Play()`가 파일이 없으면 예외 없이 조용히 무시하도록 방어되어 있어서, 이 상태로 빌드/실행해도
  기존 동작(벨소리 포함)에는 영향이 없음 — 파일만 넣으면 그 순간부터 코드 수정 없이 재생됨.
- **관련 커밋**: `ca73095`(계획안) → `b06666d`(실패 사유 6개 구조 구현) → `695b213`(입실/퇴실 문구 2개 추가).

---

## 3. DB 트랜잭션이란 무엇인가

### 3.1 개념

트랜잭션은 **여러 개의 SQL 문장을 하나의 작업 단위로 묶어서, 전부 성공하거나 전부 실패(취소)하게
만드는 것**이다. (ACID 중 "원자성(Atomicity)"에 해당 — 쪼갤 수 없는 하나의 덩어리로 처리한다는 뜻.)

기본 형태:
```
BEGIN;                 -- 트랜잭션 시작
UPDATE ...;             -- 문장 1
INSERT ...;             -- 문장 2
COMMIT;                 -- 둘 다 성공했으면 여기서 실제로 DB에 반영
```
중간에 문장 2가 실패하면 `COMMIT` 대신 `ROLLBACK`을 호출해서, 이미 실행됐던 문장 1까지 포함해서
**트랜잭션 시작 이전 상태로 전부 되돌린다**. 트랜잭션이 없으면 문장 1은 이미 반영된 채로 남고
문장 2만 빠진, 앞뒤가 안 맞는 상태(inconsistent state)가 DB에 남는다.

### 3.2 왜 필요한가 (이 프로젝트 기준 예시)

예를 들어 "교육생을 삭제하면서 그 학생의 얼굴 임베딩과 출결 기록도 같이 지운다"는 기능이 있다고
하면, 아래처럼 3개 표를 건드리게 된다.
```
DELETE FROM face_embeddings WHERE student_id = ?;
DELETE FROM attendance WHERE student_id = ?;
DELETE FROM students WHERE id = ?;
```
트랜잭션 없이 이 3개를 순서대로 실행하다가, 예를 들어 두 번째 `DELETE`에서 서버가 죽거나 예외가
나면 `face_embeddings`만 지워지고 `attendance`/`students`는 그대로 남는다. 학생 데이터는 있는데
얼굴 데이터만 없는, 앞뒤가 안 맞는 상태가 되는 것이다. 트랜잭션으로 묶여 있었다면 이 경우 전부
롤백되어 "아예 삭제를 시도한 적 없는" 상태로 깨끗하게 되돌아간다.

### 3.3 지금 이 저장소의 실제 상태

`server/Data/AttendanceRepository.cs`를 직접 열어서 확인한 결과, `CreateCheckIn()`/`SetCheckOut()`을
비롯한 모든 쓰기 메서드가 `MySqlTransaction`(또는 `BeginTransaction()`) 호출 없이, 커넥션 하나 열고
SQL 문장 하나만 실행하고 끝나는 구조다 (원본/우리 포크 공통 — 이 부분은 아직 손대지 않음).

**전수 확인 (2026-09-15)**: `server/` 전체에서 `Transaction`/`BeginTransaction`/`COMMIT`/`ROLLBACK`
키워드를 검색한 결과 **일치하는 곳이 0건**이었다 (`AdminRepository.cs`, `AttendanceRepository.cs`,
`Db.cs`, `FaceEmbeddingRepository.cs`, `StudentRepository.cs` 5개 파일 전부 확인). 즉 **이
저장소에는 DB 트랜잭션이 어디에도 적용되어 있지 않다** — 개념 설명이 아니라 실제 코드 상태로
확인된 사실.

지금 당장은 각 메서드가 SQL 문장을 하나씩만 실행하므로 "여러 표를 동시에 바꾸다가 반쪽만 반영되는"
시나리오 자체가 발생하지 않는다. 하지만 앞으로 위 3.2 예시 같은 다중 테이블 작업(교육생 삭제 시
연관 데이터 정리, 또는 오늘 팀장이 추가한 통계 기능이 나중에 "통계 캐시 테이블"처럼 파생 데이터를
같이 쓰는 방향으로 확장되는 경우 등)이 추가되는 순간부터는 트랜잭션이 없으면 위와 같은 반쪽짜리
상태 문제가 실제로 발생할 수 있다.

C#(MySqlConnector)에서 트랜잭션을 쓰는 기본 형태:
```csharp
using (MySqlConnection connection = db.OpenConnection())
using (MySqlTransaction transaction = connection.BeginTransaction())
{
    try
    {
        MySqlCommand cmd1 = new MySqlCommand("DELETE FROM face_embeddings WHERE student_id = @id", connection, transaction);
        cmd1.Parameters.AddWithValue("@id", studentId);
        cmd1.ExecuteNonQuery();

        MySqlCommand cmd2 = new MySqlCommand("DELETE FROM attendance WHERE student_id = @id", connection, transaction);
        cmd2.Parameters.AddWithValue("@id", studentId);
        cmd2.ExecuteNonQuery();

        transaction.Commit();   // 둘 다 성공해야 여기서 실제 반영
    }
    catch
    {
        transaction.Rollback(); // 하나라도 실패하면 전부 되돌림
        throw;
    }
}
```

### 3.4 참고 — 오늘 확인한 "유사도 0%" 문제는 트랜잭션과 무관함

오늘 실제로 겪은 "관리자모드 사진등록 안 됨 + 입실 시 유사도 0%" 증상은 DB나 트랜잭션 문제가
아니라, **AI 서버(Python, 8001번 포트)가 아예 켜져 있지 않아서** C# 서버가 AI 서버에 연결하지 못해
생긴 문제였다 (DB 연결 자체는 `mysql` 클라이언트로 직접 접속해서 정상 확인함). 이 문서에서 다루는
"DB 트랜잭션 없음"은 별개로 존재하는, 현재 코드베이스의 구조적 특징이지 오늘 증상의 원인이 아니다 —
나중에 보고서 쓸 때 두 가지를 같은 원인으로 혼동하지 않도록 구분해서 적어야 한다.
