using MySqlConnector;

namespace AttendanceServer.Data
{
    public class AdminRepository
    {
        private readonly Db db;

        public AdminRepository(Db db)
        {
            this.db = db;
        }

        public string GetPasswordHash(string username)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT password_hash FROM admins WHERE username = @username";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@username", username);

                object result = command.ExecuteScalar();

                if (result == null)
                {
                    return null;
                }

                return result.ToString();
            }
        }

        public int Count()
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "SELECT COUNT(*) FROM admins";
                MySqlCommand command = new MySqlCommand(sql, connection);
                object result = command.ExecuteScalar();
                return (int)System.Convert.ToInt64(result);
            }
        }

        public void Insert(string username, string passwordHash)
        {
            using (MySqlConnection connection = db.OpenConnection())
            {
                string sql = "INSERT INTO admins (username, password_hash) VALUES (@username, @passwordHash)";
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.ExecuteNonQuery();
            }
        }
    }
}
