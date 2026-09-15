import os

# 소켓 서버 바인딩 주소
SERVER_HOST = os.environ.get("ATTENDANCE_AI_HOST", "0.0.0.0")
SERVER_PORT = int(os.environ.get("ATTENDANCE_AI_PORT", "8001"))

DB_HOST = os.environ.get("ATTENDANCE_DB_HOST", "127.0.0.1")
DB_PORT = int(os.environ.get("ATTENDANCE_DB_PORT", "3306"))
DB_USER = os.environ.get("ATTENDANCE_DB_USER", "attendance_app")
DB_PASSWORD = os.environ.get("ATTENDANCE_DB_PASSWORD", "AppUser!2026")
DB_NAME = os.environ.get("ATTENDANCE_DB_NAME", "attendance_system")

# 참고: 프로젝트 경로에 한글이 포함되어 있으면 OpenCV의 Haar Cascade 파일 로딩이
# 실패하는 Windows 환경 문제가 있어 "opencv" 대신 "mtcnn"을 기본값으로 사용한다.
DETECTOR_BACKEND = "mtcnn"

# [2026-09-14 추가] 사진/화면 재생으로 얼굴인식을 통과시키는 걸 막는다.
# DeepFace에 내장된 FasNet(Silent-Face-Anti-Spoofing 기반) 모델로 판별하며,
# 별도 설치 없이 기존 deepface 패키지 안에 이미 들어있다.
#
# [2026-09-14 정정] 이전에 "실제 웹캠 환경에서 모델이 오탐한다"고 판단해서
# 기본값을 꺼짐으로 바꿨었는데, 그건 진단이 틀렸다. 진짜 원인은 requirements.txt에
# torch가 빠져있어서 Fasnet 초기화 자체가 매번 ValueError로 실패하고 있었던
# 것뿐이었다(그 예외를 조용히 삼키는 코드 때문에 원인 파악이 늦어졌다 - 지금은
# recognition.py에서 모든 예외를 로그로 남기도록 고쳤다). requirements.txt에
# torch를 추가한 뒤 실제 웹캠 프레임으로 재검증했고, 정상적으로 "진짜"로
# 판별되는 것까지 확인했다. 그래서 기본값을 다시 켜짐으로 되돌린다.
ANTI_SPOOFING_ENABLED = os.environ.get("ATTENDANCE_ANTI_SPOOFING", "true").lower() != "false"

# Facenet512보다 사람 간 구별력이 더 뛰어난 ArcFace를 사용한다.
# (Additive Angular Margin Loss로 학습되어 동일인은 더 가깝게, 타인은 더 멀게 임베딩되도록 설계된 모델)
# 모델을 바꾸면 임베딩 값 자체가 완전히 달라지므로 기존에 등록된 얼굴은 재등록이 필요하다.
MODEL_NAME = "ArcFace"

# 흐릿한 프레임 판정 기준 (라플라시안 분산이 이 값보다 작으면 흐릿한 것으로 판단)
BLUR_THRESHOLD = 30.0

# 코사인 유사도 매칭 기준값 (이 값 이상이면 동일 인물 후보로 판단).
# DeepFace 공식 권장값(ArcFace 기준 코사인 거리 0.68 => 유사도 0.32)은
# Facenet512 때도 실제 환경(동일 웹캠·배경)에서는 너무 낮아 오인식이 났었기 때문에,
# 그 경험(권장 임계값의 절반 정도 거리로 강화)을 반영해 0.66으로 우선 설정한다.
# 실제 등록자/타인으로 재테스트하면서 미세 조정이 필요하다.
MATCH_THRESHOLD = 0.66

# 1위 후보와 2위 후보의 유사도 차이가 이 값보다 작으면 애매한 것으로 보고 인식 실패 처리한다.
# (등록 인원이 1명뿐이면 2위 후보가 없으므로 이 검증은 자동으로 건너뛴다.)
MARGIN_THRESHOLD = 0.05
