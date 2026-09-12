import config
import db
import recognition


def handle_health(request):
    return {"status": "ok"}


def handle_extract_embeddings(request):
    """얼굴 등록용: 여러 장의 이미지에서 유효한 얼굴 임베딩만 추출해서 반환한다."""
    images = request.get("images", [])

    embeddings = []
    for image_base64 in images:
        image = recognition.decode_base64_image(image_base64)
        embedding = recognition.extract_embedding(image)
        if embedding is not None:
            embeddings.append(embedding)

    return {
        "embeddings": embeddings,
        "total_count": len(images),
        "valid_count": len(embeddings),
        "model_name": config.MODEL_NAME,
    }


def handle_recognize(request):
    """출석 체크용: 짧은 시간 동안 찍은 여러 장의 이미지 중 가장 유사도가 높은 결과를 사용한다.
    한 프레임의 조명·각도가 안 좋아도 다른 프레임에서 인식될 기회를 주기 위함이다.
    """
    images = request.get("images", [])
    threshold = request.get("threshold")
    if threshold is None:
        threshold = config.MATCH_THRESHOLD

    known_embeddings = db.fetch_all_embeddings()

    best_student_id = None
    best_similarity = 0.0
    best_second_similarity = 0.0

    for image_base64 in images:
        image = recognition.decode_base64_image(image_base64)
        embedding = recognition.extract_embedding(image)

        if embedding is None:
            continue

        student_id, similarity, second_similarity = recognition.find_best_match(embedding, known_embeddings)

        if student_id is not None and similarity > best_similarity:
            best_student_id = student_id
            best_similarity = similarity
            best_second_similarity = second_similarity

    if best_student_id is None or best_similarity < threshold:
        if best_student_id is None:
            best_student_id = 0
        return {"matched": False, "student_id": best_student_id, "similarity": best_similarity}

    margin = best_similarity - best_second_similarity
    if margin < config.MARGIN_THRESHOLD:
        # 1위와 2위 후보가 너무 비슷해서 확신할 수 없는 경우: 미등록/인식 실패로 처리한다.
        return {"matched": False, "student_id": 0, "similarity": best_similarity}

    return {"matched": True, "student_id": best_student_id, "similarity": best_similarity}


# action 이름 -> 처리 함수
HANDLERS = {
    "health": handle_health,
    "extract_embeddings": handle_extract_embeddings,
    "recognize": handle_recognize,
}


def dispatch(request):
    """요청 딕셔너리를 받아 action에 맞는 처리 함수를 실행하고 응답 딕셔너리를 반환한다."""
    action = request.get("action")
    handler = HANDLERS.get(action)

    if handler is None:
        return {"error": "알 수 없는 action: " + str(action)}

    return handler(request)
