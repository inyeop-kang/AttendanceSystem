# 계획안: 실패 사유별 음성 안내 (TTS)

상태: **코드 구조 완료, mp3 미생성** — 2026-09-14 작성, 2026-09-15 갱신

2026-09-15 진행: Typecast API 키가 아직 없어서 실제 mp3 생성/재생 확인은 못 했지만,
"파일이 생기면 바로 재생되는" 코드 구조는 먼저 만들어 둠 (`Sound.cs`의 각 재생 함수는
파일이 없으면 조용히 무시하도록 이미 그렇게 구현되어 있어서, 이 상태로도 기존 동작을
깨지 않는다). 아래 표의 파일명으로 mp3를 `client/Assets/Sounds/`에 넣기만 하면 바로
동작한다.

## 배경

현재 클라이언트(WPF)는 서버 요청 실패나 얼굴 인식 실패 시 화면에 텍스트 메시지만 보여준다
(일부 화면은 여기에 `MessageBox` 팝업도 추가됨 — 아래 "이미 되어 있는 것" 참고).
소리로는 입실/퇴실 성공 시 효과음(`check_in.mp3`, `check_out.mp3`)만 있고,
실패 상황에는 아무 소리도 나지 않는다.

이 계획은 실패 상황에도 **사유별로 구분되는 음성 안내**를 추가하는 것을 목표로 한다.
예: 사진(정지 화면)으로 의심될 때는 "사진이 감지되었습니다"처럼, 그냥 미등록일 때와
다른 문구를 음성으로 안내.

## 이미 되어 있는 것 (이 계획이 딛고 설 기반)

- `ai-server`가 스푸핑(사진/화면 재생) 의심 시 `recognize` 응답에
  `reason: "spoof_suspected"`를 실어서 보낸다 (`ai-server/app.py`, `ai-server/recognition.py`).
- `server/RequestHandler.cs`가 이 `reason`에 따라 클라이언트에 보여줄 `Message` 문구를
  이미 구분해서 만든다 (`server/Services/AiServerClient.cs`의 `RecognizeResult.Reason`).
- `client/Services/Sound.cs`가 이미 mp3 파일을 재생하는 구조를 갖고 있다
  (`client/Assets/Sounds/` 폴더 + `MediaPlayer` 재생, 입실/퇴실용으로 사용 중).
- 로그인, 관리자 재인증, 얼굴 등록, 교육생 저장 화면은 서버 연결 실패 시
  텍스트 라벨 + `MessageBox` 팝업을 띄운다. **단, 출석 체크(키오스크) 화면은
  연속 촬영 흐름이 끊기지 않도록 팝업을 안 띄우기로 결정함** (모달 팝업은 클릭할
  때까지 화면을 멈추기 때문). 오디오는 화면을 멈추지 않으므로 이 결정과 무관하게
  키오스크 화면에도 적용 가능하다.

## 결정: 실시간 TTS API 호출이 아니라 "미리 생성 후 파일 재생"

후보로 검토한 서비스: [Typecast](https://typecast.ai/kr/text-to-speech/)
(한국어 지원, REST API + C# SDK 제공, 무료 플랜 월 30,000자, 유료 Lite $15/월~).

문구가 미리 정해진 몇 개뿐이고 매번 달라지지 않으므로(학생 이름을 부르는 등의
동적 문구가 아님), 이벤트가 발생할 때마다 Typecast API를 호출하는 대신
**문구별로 한 번씩만 Typecast로 생성해서 mp3 파일로 저장**하고, 그 이후로는
기존 `Sound.cs` 패턴대로 로컬 재생만 한다.

이유:
- 실시간 API 호출은 인터넷 연결에 의존 → 네트워크가 새로운 장애 지점이 되고,
  요청 왕복 시간만큼 안내 지연이 생긴다. 출결 키오스크처럼 즉시 반응해야 하는
  화면에는 안 맞는다.
- 문구가 고정이므로 한 번 생성해두면 그 뒤로 API 비용이 전혀 들지 않는다
  (무료 크레딧으로 충분히 커버됨).
- 나중에 "학생 이름을 직접 불러주기"처럼 매번 텍스트가 달라지는 기능이 필요해지면,
  그때는 실시간 API 호출로 전환하는 게 맞다 (C# SDK가 있으므로 그 시점에 재검토).

## 필요한 문구 목록 (코드에서 실제로 쓰이는 메시지 기준)

| 상황 | 현재 텍스트 (코드 원문) | 위치 |
|---|---|---|
| 스푸핑 의심 | "사진(정지 화면)으로 의심되어 인식이 거부되었습니다. 실제 얼굴로 다시 시도해 주세요." | `server/RequestHandler.cs` |
| 미등록/인식 실패 | "등록되지 않은 사용자입니다. (인식 실패)" | `server/RequestHandler.cs` |
| 로그인 화면 연결 실패 | "서버에 연결할 수 없습니다. 서버 실행 상태를 확인해 주세요." | `client/LoginWindow.xaml.cs` |
| 관리자 재인증 연결 실패 | "서버에 연결할 수 없습니다." | `client/Views/AdminPasswordWindow.xaml.cs` |
| 얼굴 등록 전송 실패 | "서버 전송에 실패했습니다." | `client/Views/FaceRegisterWindow.xaml.cs` |
| 교육생 저장 실패 | "저장에 실패했습니다. (교육생 번호 중복 또는 서버 오류)" | `client/Views/StudentEditWindow.xaml.cs` |
| 출석체크 서버 통신 실패 | "서버 통신에 실패했습니다." | `client/Views/AttendanceCheckView.xaml.cs` |

문구는 원문 그대로 쓰지 않고 축약하기로 결정 (2026-09-15). 실제 녹음할 축약 문구와
파일명은 아래 표로 확정한다.

## 확정된 녹음 문구 / 파일명 (2026-09-15)

| 사유 코드 (`SoundKey`) | 파일명 | 녹음할 축약 문구 | 발생 위치 |
|---|---|---|---|
| (없음, 클라이언트 로컬) | `checkin_voice.mp3` | "입실하셨습니다" | `AttendanceCheckView` (입실 성공, 이름 없이 고정 문구) |
| (없음, 클라이언트 로컬) | `checkout_voice.mp3` | "퇴실하셨습니다" | `AttendanceCheckView` (퇴실 성공, 이름 없이 고정 문구) |
| `spoof_suspected` | `spoof_suspected.mp3` | "사진이 감지되었습니다" | `AttendanceCheckView` (인식 실패, 서버가 `Reason: spoof_suspected` 응답) |
| `no_match` | `no_match.mp3` | "등록되지 않은 사용자입니다" | `AttendanceCheckView` (인식 실패, 그 외 사유) |
| (없음, 클라이언트 로컬) | `connection_error.mp3` | "서버에 연결할 수 없습니다" | `LoginWindow`, `AdminPasswordWindow` (연결 예외) |
| (없음, 클라이언트 로컬) | `send_failure.mp3` | "전송에 실패했습니다" | `FaceRegisterWindow` (연결 예외) |
| (없음, 클라이언트 로컬) | `save_failure.mp3` | "저장에 실패했습니다" | `StudentEditWindow` (연결 예외) |
| (없음, 클라이언트 로컬) | `comm_failure.mp3` | "통신에 실패했습니다" | `AttendanceCheckView` (체크인/아웃 요청 자체가 실패한 예외) |

`spoof_suspected`/`no_match`는 서버가 판단하는 사유라서, 서버가 화면 표시용 `Message`와
별도로 `AttendanceCheckResponse.SoundKey`(신규 필드, `server/Models/Dtos.cs` +
`client/Models/Dtos.cs`)에 사유 코드를 실어 보낸다. 나머지 4개는 서버 요청 자체가
실패한 클라이언트 로컬 예외라서 서버 응답과 무관하게 클라이언트가 바로 판단해서 재생한다.

## 구현 순서

1. Typecast에서 위 문구를 화자/톤 하나로 통일해서 mp3로 생성 —
   `scripts/generate_tts_assets.ps1`로 자동화함 (Typecast REST API,
   `POST https://api.typecast.ai/v1/text-to-speech`, model `ssfm-v30` 사용).
   **API 키 발급 후 사용자가 직접 실행해야 함** (외부 서비스 계정이라 Claude가 대신 못 함).
2. `client/Assets/Sounds/`에 위 표의 파일명으로 저장 — 위 스크립트가 자동으로 해줌.
3. `client/Services/Sound.cs`에 `PlayCheckIn`/`PlayCheckOut`과 같은 패턴으로
   재생 함수 6개(`PlaySpoofSuspected`/`PlayNoMatch`/`PlayConnectionError`/
   `PlaySendFailure`/`PlaySaveFailure`/`PlayCommFailure`) 추가함 — **완료.**
4. 각 화면의 실패 처리 지점(위 표의 위치)에서 해당 재생 함수 호출 한 줄 추가.
   기존 텍스트/팝업 로직은 그대로 두고 오디오만 얹음 — **완료** (5개 파일: `LoginWindow`,
   `AdminPasswordWindow`, `FaceRegisterWindow`, `StudentEditWindow`, `AttendanceCheckView`).
5. 키오스크(`AttendanceCheckView`)에도 동일하게 오디오만 추가 — 모달 팝업과 달리
   화면 흐름을 막지 않으므로 추가 가능 — **완료.**

## 남은 일

- Typecast 계정/API 키 발급 (사용자 직접 — 외부 서비스 가입이라 Claude가 대신 할 수 없음.
  https://studio.typecast.ai/developers/api).
- 목소리(화자) 선택 — https://studio.typecast.ai 보이스 라이브러리에서 `voice_id`
  확인 (`tc_`로 시작하는 문자열).
- 위 두 가지가 준비되면 프로젝트 루트에서 실행:
  ```
  $env:TYPECAST_API_KEY = "발급받은 키"
  .\scripts\generate_tts_assets.ps1 -VoiceId "tc_xxxxxxxxxxxxxxxxxxxxxxxx"
  ```
  `client/Assets/Sounds/`에 mp3 8개가 자동 생성됨. 파일만 생기면 코드 수정 없이 바로
  재생됨 (`Sound.Play()`가 파일 없으면 조용히 무시하도록 이미 방어되어 있어서, 지금
  상태로 빌드/실행해도 기존 동작은 그대로임).
- 이름을 불러주는 음성("OOO님, 입실하셨습니다"처럼 학생 이름 포함)은 매번 다른 텍스트라
  실시간 API가 필요해지는 케이스이므로 별도 계획으로 다룬다. `checkin_voice`/
  `checkout_voice`는 이름 없는 고정 문구라 이번 계획(사전 생성 방식) 범위에 포함됨.
