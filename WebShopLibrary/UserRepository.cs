using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebShopLibrary.Database;
using Microsoft.Data.SqlClient;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;

namespace WebShopLibrary
{
    public class UserRepository
    {
        private int _nextId = 1;

        private readonly DBConnection _dbConnection;
        public UserRepository(DBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public IEnumerable<User> GetAll()
        {
            var users = new List<User>();
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("SELECT * FROM Users", connection);

            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var user = new User
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Email = reader.GetString(2),
                        Password = reader.GetString(3),
                        Role = reader.GetString(4)
                    };
                    users.Add(user);
                }
            }
            finally
            {
                connection.Close();
            }
            return users;
        }

        public User? Get(int id)
        {
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("SELECT * FROM Users WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", id);
            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Email = reader.GetString(2),
                        Password = reader.GetString(3),
                        Role = reader.GetString(4)
                    };
                }
            }
            finally
            {
                connection.Close();
            }
            return null;
        }

        public User? Add(User user)
        {
            user.Validate();

            // Hash passwordet
            var hashedPassword = HashPassword(user.Password);

            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@Username, @Email, @Password, @Role); SELECT SCOPE_IDENTITY()", connection);
            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@Password", hashedPassword); // Brug det hashede password
            cmd.Parameters.AddWithValue("@Role", user.Role);

            try
            {
                connection.Open();
                user.Id = Convert.ToInt32(cmd.ExecuteScalar());
                return user;
            }
            finally
            {
                connection.Close();
            }
        }

        public void Remove(int id)
        {
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("DELETE FROM Users WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", id);
            try
            {
                connection.Open();
                cmd.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        private string HashPassword(string password)
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;

                var hash = argon2.GetBytes(32);

                var hashBytes = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                return Convert.ToBase64String(hashBytes);
            }
        }

    }
}