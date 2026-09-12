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

        public void Insert(int studentId, string embeddingJson, string modelName)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "INSERT INTO face_embeddings (student_id, embedding, model_name) " +
                             "VALUES (@studentId, @embedding, @modelName)";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@embedding", embeddingJson);
                command.Parameters.AddWithValue("@modelName", modelName);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteByStudentId(int studentId)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "DELETE FROM face_embeddings WHERE student_id = @studentId";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.ExecuteNonQuery();
            }
        }
    }
}
