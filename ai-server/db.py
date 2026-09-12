import json

import pymysql

import config


def get_connection():
    conn = pymysql.connect(
        host=config.DB_HOST,
        port=config.DB_PORT,
        user=config.DB_USER,
        password=config.DB_PASSWORD,
        database=config.DB_NAME,
        charset="utf8mb4",
    )
    return conn


def fetch_all_embeddings():
    """현재 설정된 모델(config.MODEL_NAME)로 등록된 얼굴 임베딩만 (student_id, embedding_list) 형태로 반환한다.
    모델이 다르면 임베딩 차원이 달라 비교할 수 없으므로 model_name으로 걸러낸다.
    """
    conn = get_connection()
    try:
        cursor = conn.cursor()
        cursor.execute(
            "SELECT student_id, embedding FROM face_embeddings WHERE model_name = %s",
            (config.MODEL_NAME,),
        )
        rows = cursor.fetchall()
        result = []
        for row in rows:
            student_id = row[0]
            embedding = json.loads(row[1])
            result.append((student_id, embedding))
        return result
    finally:
        conn.close()
