using System.Collections.Generic;
using System.Text;
using MySqlConnector;

namespace AttendanceServer.Data
{
    public class FaceEmbeddingRepository
    {
        private readonly Db db;

        public FaceEmbeddingRepository(Db db)
        {
            this.db = db;
        }

        // 얼굴 (재)등록: 기존 임베딩 삭제 → 새 임베딩 저장 → students.has_face_registered 표시를
        // 하나의 트랜잭션으로 처리한다. 중간에 실패하면 전부 롤백되므로
        // "등록 표시는 되어 있는데 임베딩이 없거나 일부만 있는" 상태가 생기지 않는다.
        // students 테이블 갱신이 이 클래스에 있는 이유도 같은 트랜잭션에 묶어야 하기 때문이다.
        public void ReplaceForStudent(int studentId, List<string> embeddingJsonList, string modelName)
        {
            if (embeddingJsonList.Count == 0)
            {
                // 저장할 임베딩이 없으면 기존 등록을 지우지 않고 그대로 둔다.
                return;
            }

            using (MySqlConnection connection = db.OpenConnection())
            using (MySqlTransaction transaction = connection.BeginTransaction())
            {
                string deleteSql = "DELETE FROM face_embeddings WHERE student_id = @studentId";
                MySqlCommand deleteCommand = new MySqlCommand(deleteSql, connection, transaction);
                deleteCommand.Parameters.AddWithValue("@studentId", studentId);
                deleteCommand.ExecuteNonQuery();

                // 임베딩 여러 개를 INSERT 한 번으로 저장한다. VALUES (...), (...), ...
                StringBuilder insertSql = new StringBuilder();
                insertSql.Append("INSERT INTO face_embeddings (student_id, embedding, model_name) VALUES ");

                MySqlCommand insertCommand = new MySqlCommand("", connection, transaction);
                insertCommand.Parameters.AddWithValue("@studentId", studentId);
                insertCommand.Parameters.AddWithValue("@modelName", modelName);

                for (int i = 0; i < embeddingJsonList.Count; i++)
                {
                    if (i > 0)
                    {
                        insertSql.Append(", ");
                    }

                    insertSql.Append("(@studentId, @embedding").Append(i).Append(", @modelName)");
                    insertCommand.Parameters.AddWithValue("@embedding" + i, embeddingJsonList[i]);
                }

                insertCommand.CommandText = insertSql.ToString();
                insertCommand.ExecuteNonQuery();

                string markSql = "UPDATE students SET has_face_registered = TRUE WHERE id = @studentId";
                MySqlCommand markCommand = new MySqlCommand(markSql, connection, transaction);
                markCommand.Parameters.AddWithValue("@studentId", studentId);
                markCommand.ExecuteNonQuery();

                transaction.Commit();
            }
        }
    }
}
