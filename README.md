# AI 출결관리 시스템

웹캠으로 얼굴을 인식해 교육생의 입·퇴실을 기록하는 데스크톱 시스템입니다.
WPF 클라이언트, C# 메인 서버, Python AI 서버 3개 프로세스로 구성되며 모두 TCP 소켓으로 직접 통신합니다.

## 설계 개요

**HTTP 프레임워크 없이 소켓을 직접 구현했습니다.** 메시지 형식은
`[4바이트 길이(빅엔디안)][그 길이만큼의 UTF-8 JSON]`이고, JSON의 `action` 필드
(`login`, `getStudents`, `checkIn` 등)로 요청 종류를 구분합니다. TCP는 스트림이라 메시지 경계가
보장되지 않으므로 길이 접두사를 붙이고, 요청한 바이트 수를 모두 받을 때까지 반복해서 읽습니다
(`server/SocketProtocol.cs`의 `ReadExact`). 한 연결에서 여러 요청을 주고받고, 연결마다
스레드/태스크를 띄워 동시 요청을 처리합니다.

**AI 서버를 별도 프로세스로 분리했습니다.** 얼굴 인식은 Python 생태계(DeepFace)가 필요하지만
업무 로직과 DB는 C#으로 다루는 편이 유리합니다. 프로세스를 나누면 무거운 모델 로딩이
메인 서버의 기동·응답과 분리되고, AI 서버를 다른 PC(GPU 장비 등)로 옮겨도 주소만 바꾸면 됩니다.

**인식 정확도는 임계값 하나에 의존하지 않습니다.** 코사인 유사도가 기준값(`MATCH_THRESHOLD = 0.66`)을
넘어도, 1위와 2위 후보의 유사도 차이가 `MARGIN_THRESHOLD = 0.05` 미만이면 "애매함"으로 보고
인식 실패 처리합니다. 타인을 등록자로 오인식하는 것이 인식 실패보다 치명적이기 때문입니다.
촬영 단계에서도 라플라시안 분산으로 흐릿한 프레임을 먼저 걸러냅니다.

**모델은 ArcFace를 사용합니다.** Additive Angular Margin Loss로 학습되어 동일인은 더 가깝게,
타인은 더 멀게 임베딩되도록 설계된 모델이라 Facenet512보다 사람 간 구별력이 좋습니다.
모델을 바꾸면 임베딩 값 자체가 달라지므로 기존 등록 얼굴은 재등록이 필요하며,
`face_embeddings.model_name`으로 걸러 차원이 다른 임베딩과 비교하지 않도록 합니다.

**데이터 정합성은 DB 제약과 트랜잭션으로 보장합니다.** 얼굴 재등록은
`기존 임베딩 삭제 → 새 임베딩 저장 → 등록 표시` 세 단계를 한 트랜잭션으로 처리해
중간에 실패해도 "등록 표시는 되어 있는데 임베딩이 없는" 상태가 생기지 않습니다.
입실 중복은 `uniq_student_date` UNIQUE 제약으로, 퇴실 시각 덮어쓰기는
UPDATE의 `check_out_time IS NULL` 조건으로 막습니다. 조회한 뒤 저장하는 방식은
그 사이에 들어온 동시 요청을 막지 못하므로 판정을 DB에 맡겼습니다.

## 구성

| 디렉터리 | 역할 | 기술 | 포트 |
|---|---|---|---|
| `client/` | WPF 클라이언트 (관리자 모드 / 출석 키오스크) | .NET 10, WPF, OpenCvSharp4 | — |
| `server/` | 메인 서버. 인증·교육생·출결 업무 로직과 DB 접근 | .NET 10, MySqlConnector, BCrypt | 5080 |
| `ai-server/` | 얼굴 임베딩 추출과 매칭 | Python, DeepFace(ArcFace), MTCNN | 8001 |
| `db/` | MySQL 스키마 (`schema.sql`) | MySQL, InnoDB | 3306 |

```
WPF 클라이언트 ──TCP 5080──> C# 메인 서버 ──TCP 8001──> Python AI 서버
                                  │                          │
                                  └──────── MySQL 3306 ──────┘
```

DB 테이블은 `admins`, `students`, `face_embeddings`, `attendance` 4개입니다.
교육생 1명당 임베딩은 여러 개 저장하고(촬영 각도·조명 편차 흡수), 출결은 `(student_id, attendance_date)`에
UNIQUE를 걸어 하루 1행으로 관리합니다.

## 실행

MySQL이 실행 중이어야 하고, 아래 순서대로 각각 다른 터미널에서 실행합니다.
기본 설정은 모든 구성요소가 같은 PC에 있는 경우를 가정하므로 환경변수 없이 그대로 동작합니다.

**1. 데이터베이스**

```powershell
mysql -u root -p
```
```sql
SOURCE db/schema.sql;
```

`attendance_system` DB와 테이블 4개, 애플리케이션 계정(`attendance_app`)이 생성됩니다.
관리자 테이블이 비어 있으면 C# 서버가 기동할 때 초기 계정(`admin`)을 시드합니다.

**2. AI 서버** — 최초 실행 시 ArcFace 가중치를 자동으로 내려받습니다.

```powershell
cd ai-server
python -m venv .venv
.venv\Scripts\pip install -r requirements.txt
.venv\Scripts\python main.py
```

**3. 메인 서버**

```powershell
dotnet run --project server/AttendanceServer.csproj
```

**4. 클라이언트**

```powershell
dotnet run --project client/AttendanceClient.csproj
```

Visual Studio에서 작업할 때는 `AttendanceSystem.slnx`를 열면 클라이언트와 서버 프로젝트가 함께 열립니다.
(AI 서버는 포함되지 않으므로 따로 실행합니다.)

## 설정

기본값을 바꿀 때만 환경변수를 지정합니다. 환경변수는 그것을 설정한 터미널에서 시작한 프로세스에만 적용되므로,
서버를 이미 실행 중이었다면 종료 후 다시 실행해야 반영됩니다.

| 환경변수 | 적용 대상 | 기본값 | 설명 |
|---|---|---|---|
| `ATTENDANCE_SERVER_HOST` | 클라이언트 | `127.0.0.1` | 접속할 메인 서버 주소 |
| `ATTENDANCE_SERVER_PORT` | 클라이언트, 메인 서버 | `5080` | 메인 서버 포트 |
| `ATTENDANCE_AI_HOST` | 메인 서버 | `127.0.0.1` | 접속할 AI 서버 주소 |
| `ATTENDANCE_AI_HOST` | AI 서버 | `0.0.0.0` | 수신할 인터페이스 주소 |
| `ATTENDANCE_AI_PORT` | 메인 서버, AI 서버 | `8001` | AI 서버 포트 |
| `ATTENDANCE_DB_CONNECTION` | 메인 서버 | 아래 참고 | MySqlConnector 연결 문자열 |
| `ATTENDANCE_DB_HOST` / `_PORT` / `_USER` / `_PASSWORD` / `_NAME` | AI 서버 | `127.0.0.1` / `3306` / `attendance_app` / — / `attendance_system` | AI 서버의 DB 접속 정보 |

`ATTENDANCE_AI_HOST`는 **메인 서버에서는 접속할 주소, AI 서버에서는 수신할 주소**라는 점에 주의합니다.
메인 서버 쪽에 `0.0.0.0`을 넣으면 안 됩니다.

주소 기본값이 `localhost`가 아니라 `127.0.0.1`인 이유는 의도적입니다. 자체 소켓 서버들이 IPv4로만
리스닝하는데 Windows는 `localhost`를 IPv6(`::1`)로 먼저 시도해서, 요청마다 약 2초의 타임아웃이
붙었던 문제가 있었습니다 (`TROUBLESHOOTING.md` 1번).

DB 비밀번호는 `schema.sql`·`server/Config.cs`·`ai-server/config.py`에 개발용 기본값이 들어 있습니다.
실제 운영에 쓴다면 DB 계정 비밀번호를 바꾸고 위 환경변수로 주입해야 합니다.

```powershell
$env:ATTENDANCE_DB_CONNECTION = "Server=127.0.0.1;Port=3306;Database=attendance_system;User=attendance_app;Password=<비밀번호>;"
```

다른 PC로 나눠 실행할 때는 각 구간의 주소를 해당 PC의 IP로 바꾸고, 방화벽에서 5080·8001·3306이
열려 있는지, MySQL 계정의 접속 허용 호스트(기본 스키마는 `attendance_app@localhost`)를 확인합니다.

## 사용 흐름

로그인 후 **관리자 모드**와 **출석 키오스크** 중 하나를 선택합니다.
교육생이 직접 출석을 체크하는 실사용 환경에서는 키오스크로 실행해야 합니다.
키오스크는 출석 화면만 노출하고, 관리자 화면으로 돌아가려면 관리자 비밀번호를 다시 입력해야 합니다.

1. **교육생 등록** — 관리자 모드 → 교육생 관리 → 신규 등록
2. **얼굴 등록** — 교육생 목록의 얼굴등록 버튼 → 촬영 시작. 12프레임을 연속 촬영해 AI 서버로 보내고,
   유효한 임베딩이 3개 이상일 때만 저장합니다
3. **입·퇴실** — 키오스크에서 웹캠 시작 후 입실/퇴실. 5프레임을 촬영해 그중 가장 유사도가 높은 결과를 사용합니다
4. **조회** — 출석 현황(기간별 전체), 교육생 개별 통계(출석률, 평균 입·퇴실 시각, 평균 체류시간)

출결 상태는 `status` 컬럼이 아니라 입·퇴실 시각의 유무로 판정합니다.
기록이 없으면 결석, 퇴실 시각이 있으면 퇴실, 그 외에는 입실입니다.

## 개발 참고

| 화면 | UI 파일 | 연결되는 기능 |
|---|---|---|
| 로그인 | `client/LoginWindow.xaml` | `ApiClient.Login` |
| 모드 선택 | `client/ModeSelectWindow.xaml` | 관리자 창 / 키오스크 창 |
| 출석 현황 | `client/Views/AttendanceTodayView.xaml` | `GetAttendanceSummary` |
| 교육생 개별 통계 | `client/Views/StudentStatsView.xaml` | `GetStudentStats` |
| 교육생 관리 | `client/Views/StudentsView.xaml` | 검색·등록·수정·삭제 |
| 교육생 등록·수정 | `client/Views/StudentEditWindow.xaml` | `CreateStudent` / `UpdateStudent` |
| 얼굴 등록 | `client/Views/FaceRegisterWindow.xaml` | `RegisterFace` |
| 출석 처리 | `client/Views/AttendanceCheckView.xaml` | `CheckIn` / `CheckOut` |
| 관리자 재인증 | `client/Views/AdminPasswordWindow.xaml` | 키오스크 → 관리자 모드 전환 |

서버 요청은 `client/Services/ApiClient.cs`, 웹캠 처리는 `client/Services/WebcamCapture.cs`에 모여 있습니다.
서버 쪽 `action` 분기는 `server/RequestHandler.cs` 한 곳에서 이뤄지고, DB 접근은 `server/Data/`의
레포지토리 4개로 나뉩니다. 인식 임계값·검출기·프레임 관련 값은 `ai-server/config.py`에 있습니다.

UI는 [Figma 설계](https://www.figma.com/design/hTmoGgCyABZbExZ2BS0SaB?node-id=4-259)를 참고하되,
Figma와 실행 프로그램이 자동 동기화되지는 않으므로 XAML을 직접 수정하고 빌드해야 합니다.

동작 확인 절차와 자주 발생하는 문제는 [TROUBLESHOOTING.md](TROUBLESHOOTING.md)를 참고하세요.
