-- 출결관리 시스템 DB 스키마
-- 실행: mysql -u root -p < schema.sql

CREATE DATABASE IF NOT EXISTS attendance_system
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE attendance_system;

-- 관리자 계정
CREATE TABLE IF NOT EXISTS admins (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    username      VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- 교육생 기본 정보
CREATE TABLE IF NOT EXISTS students (
    id                    INT AUTO_INCREMENT PRIMARY KEY,
    student_no            VARCHAR(20)  NOT NULL UNIQUE,
    name                  VARCHAR(50)  NOT NULL,
    department            VARCHAR(100),
    grade                 INT,
    registered_at         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    has_face_registered   BOOLEAN      NOT NULL DEFAULT FALSE
) ENGINE=InnoDB;

-- 얼굴 임베딩 (교육생 1명당 여러 개 저장 가능)
CREATE TABLE IF NOT EXISTS face_embeddings (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    student_id  INT NOT NULL,
    embedding   LONGTEXT NOT NULL,      -- JSON 배열(float) 문자열로 저장
    model_name  VARCHAR(50) NOT NULL DEFAULT 'Facenet',
    created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_face_student FOREIGN KEY (student_id)
        REFERENCES students(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 출결 기록 (교육생 1명당 하루 1행)
CREATE TABLE IF NOT EXISTS attendance (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    student_id      INT NOT NULL,
    attendance_date DATE NOT NULL,
    check_in_time   DATETIME NULL,
    check_out_time  DATETIME NULL,
    -- 화면에 표시되는 상태(입실/퇴실/결석)는 이 컬럼이 아니라
    -- check_in_time / check_out_time 의 유무로 판단한다. (지각 개념은 사용하지 않음)
    status          ENUM('present','absent') NOT NULL DEFAULT 'present',
    confidence      FLOAT NULL,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_attendance_student FOREIGN KEY (student_id)
        REFERENCES students(id) ON DELETE CASCADE,
    CONSTRAINT uniq_student_date UNIQUE (student_id, attendance_date)
) ENGINE=InnoDB;

-- 애플리케이션 전용 DB 계정 (C# 서버, Python AI 서버가 사용)
CREATE USER IF NOT EXISTS 'attendance_app'@'localhost' IDENTIFIED BY 'AppUser!2026';
GRANT SELECT, INSERT, UPDATE, DELETE ON attendance_system.* TO 'attendance_app'@'localhost';
FLUSH PRIVILEGES;

-- 기본 관리자 계정: admin / admin1234
-- 비밀번호 해시는 BCrypt로 서버 시작 시 시드되므로 여기서는 넣지 않음 (server/Program.cs 참고)
