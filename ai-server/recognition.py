import base64

import cv2
import numpy as np
from deepface import DeepFace

import config


def decode_base64_image(base64_str):
    """base64 문자열을 OpenCV 이미지(BGR numpy 배열)로 변환한다."""
    if "," in base64_str:
        base64_str = base64_str.split(",")[1]
    image_bytes = base64.b64decode(base64_str)
    image_array = np.frombuffer(image_bytes, dtype=np.uint8)
    image = cv2.imdecode(image_array, cv2.IMREAD_COLOR)
    return image


def is_blurry(image):
    """라플라시안 분산으로 흐릿한 이미지인지 판단한다."""
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    variance = cv2.Laplacian(gray, cv2.CV_64F).var()
    return variance < config.BLUR_THRESHOLD


def extract_embedding(image):
    """이미지 한 장에서 얼굴을 검출하고 임베딩을 추출한다.
    얼굴이 없거나 흐릿하면 None을 반환한다.
    """
    if image is None:
        return None

    if is_blurry(image):
        return None

    try:
        results = DeepFace.represent(
            img_path=image,
            model_name=config.MODEL_NAME,
            detector_backend=config.DETECTOR_BACKEND,
            enforce_detection=True,
        )
    except Exception:
        return None

    if len(results) == 0:
        return None

    # 여러 얼굴이 검출되면 가장 넓은 얼굴 영역(본인일 가능성이 높음) 하나만 사용한다.
    best = results[0]
    best_area = 0
    for item in results:
        region = item["facial_area"]
        area = region["w"] * region["h"]
        if area > best_area:
            best_area = area
            best = item

    return best["embedding"]


def cosine_similarity(vector_a, vector_b):
    a = np.array(vector_a)
    b = np.array(vector_b)
    norm_a = np.linalg.norm(a)
    norm_b = np.linalg.norm(b)
    if norm_a == 0 or norm_b == 0:
        return 0.0
    similarity = float(np.dot(a, b) / (norm_a * norm_b))
    return similarity


def find_best_match(embedding, known_embeddings):
    """known_embeddings: [(student_id, embedding_list), ...]
    학생별로 가장 높은 유사도를 대표값으로 삼아, 1위 학생과 그 유사도, 2위 학생의 유사도를 반환한다.
    (1위 student_id, 1위 유사도, 2위 유사도) 형태이며 등록자가 없으면 (None, 0.0, 0.0)이다.
    2위 후보가 없으면(등록자가 1명뿐이면) 2위 유사도는 0.0으로 반환되어 마진 검증이 자동으로 통과된다.
    """
    best_similarity_by_student = {}

    for student_id, known_embedding in known_embeddings:
        similarity = cosine_similarity(embedding, known_embedding)
        current_best = best_similarity_by_student.get(student_id, 0.0)

        if similarity > current_best:
            best_similarity_by_student[student_id] = similarity

    top1_student_id = None
    top1_similarity = 0.0
    top2_similarity = 0.0

    for student_id, similarity in best_similarity_by_student.items():
        if similarity > top1_similarity:
            top2_similarity = top1_similarity
            top1_similarity = similarity
            top1_student_id = student_id
        elif similarity > top2_similarity:
            top2_similarity = similarity

    return top1_student_id, top1_similarity, top2_similarity
