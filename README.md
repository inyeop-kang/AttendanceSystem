# AI 출결관리 시스템

WPF 클라이언트, C# 메인 서버, Python AI 서버를 연결해 교육생의 얼굴 등록과 출결을 관리하는 프로젝트입니다.

## 구성

```
db/            MySQL 스키마 (schema.sql)
ai-server/     Python 소켓 서버 + DeepFace 얼굴인식 (main.py, app.py, protocol.py)
server/        C# TCP 소켓 서버 (AttendanceServer.csproj)
client/        WPF 클라이언트, 관리자용 프로그램 (AttendanceClient.csproj)
AttendanceSystem.slnx   server·client 프로젝트를 함께 여는 솔루션 파일
```

모든 통신(WPF ↔ C# 서버, C# 서버 ↔ AI 서버)은 HTTP가 아니라 TCP/IP 소켓을 직접 사용합니다.
메시지 형식은 `[4바이트 길이(빅엔디안)][그 길이만큼의 UTF-8 JSON 텍스트]`이며,
JSON에 담긴 `action` 필드(예: `login`, `getStudents`, `checkIn`)로 요청 종류를 구분합니다.

## 1. 데이터베이스 준비

MySQL 서버가 실행 중이어야 합니다.

프로젝트 루트에서 MySQL 클라이언트를 실행합니다. 아래 방식은 PowerShell에서도 사용할 수 있습니다.

```powershell
mysql -u root -p
```

MySQL 프롬프트에서 실행합니다.

```sql
SOURCE db/schema.sql;
```

이미 DB가 있다면 적용 전 `schema.sql`을 검토하세요. `CREATE USER IF NOT EXISTS`는 기존 계정의 비밀번호를 변경하지 않으므로 서버 설정과 실제 DB 계정 정보가 일치해야 합니다.

- `attendance_system` 데이터베이스와 `students`, `face_embeddings`, `attendance`, `admins` 테이블이 생성됩니다.
- 관리자 테이블이 비어 있으면 C# 서버의 `SeedAdmin`에서 초기 계정을 생성합니다. 로그인 정보는 프로젝트 관리자에게 확인하세요.

## 2. AI 서버 실행 (Python)

```
cd ai-server
python -m venv venv
venv\Scripts\pip install -r requirements.txt
venv\Scripts\python main.py
```

- 최초 실행 시 DeepFace가 얼굴인식 모델(ArcFace) 가중치를 자동으로 다운로드합니다.
- 기본적으로 `0.0.0.0:8001`에서 TCP 소켓으로 대기합니다.
- `config.py`에서 DB 접속 정보와 매칭 임계값(`MATCH_THRESHOLD`)을 조정할 수 있습니다.
- 얼굴 검출기(`DETECTOR_BACKEND`)는 기본값을 `mtcnn`으로 설정했습니다. `opencv`로 바꾸지 마세요 —
  프로젝트 경로(`E:\출결관리`)에 한글이 포함되어 있어서 OpenCV의 Haar Cascade 파일 로딩이
  Windows에서 깨지는 문제가 있습니다(경로에 비ASCII 문자가 있으면 발생하는 OpenCV의 알려진 제약).

## 3. C# 서버 실행

```
cd server
dotnet run
```

- 기본적으로 `0.0.0.0:5080`에서 TCP 소켓으로 대기합니다.
- `Config.cs`에서 DB 접속 문자열, AI 서버 주소, 지각 기준 시각을 설정합니다 (환경변수로 덮어쓰기 가능:
  `ATTENDANCE_SERVER_PORT`, `ATTENDANCE_DB_CONNECTION`, `ATTENDANCE_AI_HOST`, `ATTENDANCE_AI_PORT`).

## 4. WPF 클라이언트 실행

```
cd client
dotnet run
```

- 로그인 화면에서 프로젝트 관리자가 안내한 관리자 계정으로 로그인합니다.
- 로그인 후 "관리자 모드"(전체 메뉴) 또는 "출석 키오스크 시작"(얼굴인식 출석체크 화면만, 다른 화면 접근 불가) 중 선택합니다.
  - 교육생이 직접 출석 체크를 하는 실사용 환경에서는 항상 "출석 키오스크"로 실행해야 교육생 관리·대시보드 등에 접근하지 못합니다.
  - 키오스크 화면 우측 상단의 "관리자 모드로 전환" 버튼은 관리자 비밀번호를 입력해야만 동작합니다.
- 관리자 모드에서 교육생 관리 → 신규 등록 → (등록 직후 뜨는 얼굴 등록 안내에서 예 선택) 순으로 교육생을 등록합니다.

## 5. Figma 디자인과 WPF 연결

[Figma UI 설계](https://www.figma.com/design/hTmoGgCyABZbExZ2BS0SaB?node-id=4-259)를 참고해 WPF 화면을 구현합니다.

Figma 파일은 실행 프로그램과 자동 동기화되지 않습니다. 디자인을 수정한 뒤에는 해당 XAML을 수정하고 빌드해야 합니다.

| 구분 | 역할 | 관련 파일 |
|---|---|---|
| Figma | 레이아웃, 색상, 간격, 화면 상태 설계 | 위 Figma 링크 |
| XAML | 실제 버튼·입력창·표·영상 영역 구성 | `client/*.xaml`, `client/Views/*.xaml` |
| C# 화면 코드 | 클릭 이벤트, 입력 검증, 결과 표시 | 각 화면의 `.xaml.cs` |
| 통신 서비스 | 메인 서버에 TCP 요청 전송 | `client/Services/ApiClient.cs` |
| 웹캠 서비스 | 카메라 열기, 프레임 읽기, 이미지 변환 | `client/Services/WebcamCapture.cs` |

예를 들어 로그인 버튼은 XAML의 `Click="LoginButton_Click"`으로 C# 함수와 연결됩니다. 해당 함수는 아이디·비밀번호를 읽어 `ApiClient.Login()`을 호출합니다.

### UI 변경본을 적용하는 순서

1. Visual Studio에서 `AttendanceSystem.slnx`를 엽니다.
2. 디자인에 맞춰 해당 XAML의 배치와 스타일을 수정합니다.
3. 기존 `x:Name`, `Click`, `Loaded`, `Unloaded`, `Closing` 연결을 확인합니다.
4. `AttendanceClient`를 시작 프로젝트로 지정해 빌드·실행합니다.
5. 서버를 실행한 상태에서 로그인, 목록 조회, 등록·수정·삭제를 확인합니다.
6. 웹캠과 AI 서버까지 준비한 뒤 얼굴 등록과 입실·퇴실을 확인합니다.

별도로 전달받은 UI 수정본은 변경 파일을 비교해 적용해야 합니다. Figma 링크나 이 README만 업데이트해도 해당 UI가 설치되는 것은 아닙니다. 화면 크기와 스타일은 실제 체크아웃한 소스가 기준입니다.

### 화면과 기능 연결 위치

| 화면 | UI 파일 | 연결하는 기능 |
|---|---|---|
| 로그인 | `client/LoginWindow.xaml` | `LoginButton_Click` → `ApiClient.Login` |
| 모드 선택 | `client/ModeSelectWindow.xaml` | 관리자 창 또는 키오스크 창 열기 |
| 출석 현황 | `client/Views/AttendanceTodayView.xaml` | 기간 선택 → `ApiClient.GetAttendanceSummary` |
| 교육생 관리 | `client/Views/StudentsView.xaml` | 검색·등록·수정·삭제·얼굴 등록 창 열기 |
| 교육생 등록·수정 | `client/Views/StudentEditWindow.xaml` | `CreateStudent` 또는 `UpdateStudent` |
| 얼굴 등록 | `client/Views/FaceRegisterWindow.xaml` | 연속 촬영 → `ApiClient.RegisterFace` |
| 출석 처리 | `client/Views/AttendanceCheckView.xaml` | 웹캠 시작·정지 → `CheckIn` 또는 `CheckOut` |
| 관리자 재인증 | `client/Views/AdminPasswordWindow.xaml` | 키오스크에서 관리자 모드로 전환할 때 비밀번호 확인 |

## 6. 서버 주소와 환경변수 설정

아래 명령은 프로젝트 루트의 PowerShell 기준입니다. 서버마다 별도 터미널을 사용하며 환경변수는 해당 터미널에서 시작한 프로세스에 적용됩니다.

### 같은 PC에서 실행

별도로 주소를 변경하지 않았다면 클라이언트와 C# 서버는 `localhost`로 연결합니다.

| 구간 | 기본 주소·포트 | 설정 파일 |
|---|---|---|
| WPF → C# 서버 | `localhost:5080` | `client/Services/Config.cs` |
| C# 서버 → AI 서버 | `localhost:8001` | `server/Config.cs` |
| C# 서버 → MySQL | `localhost:3306` | `server/Config.cs` |
| AI 서버 → MySQL | `localhost:3306` | `ai-server/config.py` |

### C# 서버 터미널

DB 비밀번호는 자신의 실제 값으로 바꾸고 문서나 커밋에 실제 운영 비밀번호를 넣지 않습니다.

```powershell
$env:ATTENDANCE_DB_CONNECTION = "Server=localhost;Port=3306;Database=attendance_system;User=attendance_app;Password=YOUR_DB_PASSWORD;"
$env:ATTENDANCE_SERVER_PORT = "5080"
$env:ATTENDANCE_AI_HOST = "localhost"
$env:ATTENDANCE_AI_PORT = "8001"
dotnet run --project server/AttendanceServer.csproj
```

### Python AI 서버 터미널

가상환경을 준비한 뒤 실행합니다. C# 서버와 동일한 DB를 가리키도록 설정합니다.

```powershell
$env:ATTENDANCE_DB_HOST = "localhost"
$env:ATTENDANCE_DB_PORT = "3306"
$env:ATTENDANCE_DB_USER = "attendance_app"
$env:ATTENDANCE_DB_PASSWORD = "YOUR_DB_PASSWORD"
$env:ATTENDANCE_DB_NAME = "attendance_system"
$env:ATTENDANCE_AI_HOST = "0.0.0.0"
$env:ATTENDANCE_AI_PORT = "8001"
Set-Location ai-server
.\venv\Scripts\python.exe main.py
```

`ATTENDANCE_AI_HOST`는 C# 서버에서는 **접속할 AI 서버 주소**, Python에서는 **수신할 인터페이스 주소**입니다. 별도 터미널에서 설정해야 하며 C# 서버의 접속 대상으로 `0.0.0.0`을 입력하지 않습니다.

### 클라이언트 터미널

```powershell
$env:ATTENDANCE_SERVER_HOST = "localhost"
$env:ATTENDANCE_SERVER_PORT = "5080"
dotnet run --project client/AttendanceClient.csproj
```

다른 PC의 C# 서버에 연결할 때는 `ATTENDANCE_SERVER_HOST`에 그 PC의 IP 주소를 입력합니다. AI 서버가 다른 PC에 있으면 C# 서버의 `ATTENDANCE_AI_HOST`도 그 주소로 변경합니다. 해당 PC의 방화벽과 네트워크에서 필요한 포트가 연결 가능한지 확인합니다.

MySQL도 다른 PC에서 실행한다면 두 서버의 DB 주소와 MySQL 계정의 접속 허용 호스트를 함께 확인해야 합니다. 기본 스키마의 DB 계정은 `attendance_app@localhost`입니다.

## 7. 단계별 동작 확인

### 캠 없이 확인할 수 있는 범위

| 준비 상태 | 확인 가능 |
|---|---|
| 클라이언트만 실행 | 로그인 화면 표시, 빈 입력 안내 |
| MySQL + C# 서버 + 클라이언트 | 로그인, 모드 전환, 교육생 검색·등록·수정·삭제, 출석 기록 조회 |
| MySQL + C# 서버 + AI 서버 + 웹캠 + 클라이언트 | 얼굴 등록, 얼굴 인식, 입실·퇴실 기록 |

교육생을 신규 등록한 뒤 얼굴 등록 여부를 물으면 캠이 없는 경우 **아니요**를 선택합니다. 나중에 교육생 목록의 얼굴등록 버튼으로 촬영할 수 있습니다.

현재 소스에는 가상 데이터를 사용하는 시연 모드나 이미지 파일 업로드 인식 기능이 없습니다. 캠이 없다고 영상 영역을 삭제할 필요는 없으며, 실제 인식 대신 수행한 화면 검증을 얼굴 인식 성공으로 설명하지 않습니다.

### 얼굴 인식 확인 순서

1. 웹캠을 연결하고 다른 프로그램이 카메라를 사용 중인지 확인합니다.
2. MySQL, AI 서버, C# 서버, 클라이언트를 실행합니다.
3. 관리자 로그인 후 교육생을 등록합니다.
4. 교육생 목록에서 **얼굴등록**을 선택합니다.
5. 얼굴 등록 창에서 **촬영 시작**을 누르고 안내에 따라 촬영합니다.
6. 등록 성공을 확인한 뒤 출석 키오스크로 전환합니다.
7. **웹캠 시작** → **입실**을 눌러 인식 결과를 확인합니다.
8. **퇴실**을 눌러 처리 결과를 확인합니다.
9. 관리자 비밀번호를 입력해 관리자 모드로 돌아가 해당 날짜의 출결 기록을 조회합니다.

현재 코드에서 얼굴 등록은 12프레임, 입실·퇴실 인식은 5프레임을 수집합니다. 실제 걸리는 시간은 영상 수신과 서버 처리 시간에 따라 달라집니다.

## 8. 자주 확인할 문제

| 증상 | 확인할 내용 |
|---|---|
| 서버에 연결할 수 없음 | C# 서버 실행 여부, 서버 주소, 포트 5080, 방화벽 |
| C# 서버 시작 실패 | MySQL 실행 여부, 스키마, DB 계정·비밀번호, 포트 충돌 |
| 로그인 실패 | 관리자 계정 정보와 DB 연결 확인. 기본 계정은 관리자 테이블이 비었을 때 시드됨 |
| 웹캠을 열 수 없음 | 캠 연결, Windows 카메라 접근 설정, 다른 프로그램의 캠 점유 |
| 얼굴 등록·인식 요청 실패 | AI 서버 실행 여부, C# 서버의 AI 주소·포트, Python 터미널 로그 |
| 인식 실패 또는 미등록 | 해당 교육생의 얼굴 등록 여부, 조명·초점·얼굴 가림, 촬영 프레임 |
| UI를 바꿨는데 화면이 같음 | 수정한 프로젝트가 시작 프로젝트인지, 새로 빌드했는지 확인 |
| DB 설정을 바꿨는데 반영되지 않음 | 실행 중인 서버를 종료하고 환경변수를 설정한 터미널에서 다시 실행 |

## 실행 순서 요약

1. MySQL 실행 확인 → `schema.sql` 적용
2. AI 서버 실행 (포트 8001)
3. C# 서버 실행 (포트 5080, 관리자 계정 자동 시드)
4. WPF 클라이언트 실행 → 로그인 → 교육생 등록(얼굴 등록 포함) → 출석 처리 테스트
