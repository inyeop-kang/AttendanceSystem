# AI 출결관리 시스템

`attendance_system_plan.md` 문서를 기반으로 구현한 얼굴인식 출결관리 시스템입니다.

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

```
mysql -u root -p < db/schema.sql
```

- `attendance_system` 데이터베이스와 `students`, `face_embeddings`, `attendance`, `admins` 테이블이 생성됩니다.
- 앱 전용 계정 `attendance_app` (비밀번호 `AppUser!2026`) 이 함께 생성됩니다.
- 기본 관리자 계정(`admin` / `admin1234`)은 C# 서버를 처음 실행할 때 자동으로 생성됩니다.

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

- 로그인 화면에서 `admin` / `admin1234`로 로그인합니다.
- 로그인 후 "관리자 모드"(전체 메뉴) 또는 "출석 키오스크 시작"(얼굴인식 출석체크 화면만, 다른 화면 접근 불가) 중 선택합니다.
  - 교육생이 직접 출석 체크를 하는 실사용 환경에서는 항상 "출석 키오스크"로 실행해야 교육생 관리·대시보드 등에 접근하지 못합니다.
  - 키오스크 화면 우측 상단의 "관리자 모드로 전환" 버튼은 관리자 비밀번호를 입력해야만 동작합니다.
- 관리자 모드에서 교육생 관리 → 신규 등록 → (등록 직후 뜨는 얼굴 등록 안내에서 예 선택) 순으로 교육생을 등록합니다.

## 실행 순서 요약

1. MySQL 실행 확인 → `schema.sql` 적용
2. AI 서버 실행 (포트 8001)
3. C# 서버 실행 (포트 5080, 관리자 계정 자동 시드)
4. WPF 클라이언트 실행 → 로그인 → 교육생 등록(얼굴 등록 포함) → 출석 처리 테스트
