using System;
using System.Collections.Generic;
using MySqlConnector;
using AttendanceServer.Models;

namespace AttendanceServer.Data
{
    public class StudentRepository
    {
        private readonly Db db;

        public StudentRepository(Db db)
        {
            this.db = db;
        }

        private Student ReadStudent(MySqlDataReader reader)
        {
            Student student = new Student();
            student.Id = reader.GetInt32("id");
            student.StudentNo = reader.GetString("student_no");
            student.Name = reader.GetString("name");
            student.Department = Db.GetStringOrEmpty(reader, "department");
            student.Grade = Db.GetIntOrZero(reader, "grade");
            student.RegisteredAt = reader.GetDateTime("registered_at");
            student.HasFaceRegistered = reader.GetBoolean("has_face_registered");
            return student;
        }

        public List<Student> Search(string keyword, string department)
        {
            List<Student> list = new List<Student>();

            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT id, student_no, name, department, grade, registered_at, has_face_registered " +
                             "FROM students WHERE 1=1 ";

                if (!string.IsNullOrEmpty(keyword))
                {
                    sql = sql + "AND (name LIKE @keyword OR student_no LIKE @keyword) ";
                }

                if (!string.IsNullOrEmpty(department))
                {
                    sql = sql + "AND department = @department ";
                }

                sql = sql + "ORDER BY id DESC";

                MySqlCommand command = new MySqlCommand(sql, connection);

                if (!string.IsNullOrEmpty(keyword))
                {
                    command.Parameters.AddWithValue("@keyword", "%" + keyword + "%");
                }

                if (!string.IsNullOrEmpty(department))
                {
                    command.Parameters.AddWithValue("@department", department);
                }

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(ReadStudent(reader));
                    }
                }
            }

            return list;
        }

        public Student GetById(int id)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT id, student_no, name, department, grade, registered_at, has_face_registered " +
                             "FROM students WHERE id = @id";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadStudent(reader);
                    }
                }
            }

            return null;
        }

        public Student GetByStudentNo(string studentNo)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT id, student_no, name, department, grade, registered_at, has_face_registered " +
                             "FROM students WHERE student_no = @studentNo";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentNo", studentNo);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadStudent(reader);
                    }
                }
            }

            return null;
        }

        // 새 교육생을 저장하고 id를 반환한다. 학번(student_no)이 이미 쓰이고 있으면
        // UNIQUE 제약에 걸리므로 0을 반환한다. 조회로 확인한 뒤 INSERT 하면
        // 그 사이에 들어온 동시 요청을 막지 못하기 때문에 DB 제약을 그대로 이용한다.
        public int Insert(StudentCreateRequest request)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "INSERT INTO students (student_no, name, department, grade) " +
                             "VALUES (@studentNo, @name, @department, @grade); " +
                             "SELECT LAST_INSERT_ID();";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentNo", request.StudentNo);
                command.Parameters.AddWithValue("@name", request.Name);
                command.Parameters.AddWithValue("@department", request.Department);
                command.Parameters.AddWithValue("@grade", request.Grade);

                try
                {
                    object result = command.ExecuteScalar();
                    return (int)Convert.ToInt64(result);
                }
                catch (MySqlException e)
                {
                    if (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                    {
                        return 0;
                    }

                    throw;
                }
            }
        }

        // 교육생 정보를 수정한다. 바꾸려는 학번을 다른 교육생이 이미 쓰고 있으면
        // UNIQUE 제약에 걸리므로 false를 반환한다.
        public bool Update(int id, StudentUpdateRequest request)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "UPDATE students SET student_no = @studentNo, name = @name, " +
                             "department = @department, grade = @grade WHERE id = @id";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentNo", request.StudentNo);
                command.Parameters.AddWithValue("@name", request.Name);
                command.Parameters.AddWithValue("@department", request.Department);
                command.Parameters.AddWithValue("@grade", request.Grade);
                command.Parameters.AddWithValue("@id", id);

                try
                {
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (MySqlException e)
                {
                    if (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                    {
                        return false;
                    }

                    throw;
                }
            }
        }

        public bool Delete(int id)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "DELETE FROM students WHERE id = @id";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                int affected = command.ExecuteNonQuery();
                return affected > 0;
            }
        }
    }
}
