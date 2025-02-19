using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;
using WebShopLibrary.Database;

namespace WebShopLibrary
{
    public class AuthService
    {
        private readonly DBConnection _dConnection;

        public AuthService(DBConnection dbConnection)
        {
            _dConnection = dbConnection;
        }

        // Hashes password with Argon2
        private string HashPassword(string password)
        {
            var salt = new byte[16]; // Generér en tilfældig salt
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt); // Fyld salt-arrayen med tilfældige bytes
            }

            // Brug Argon2 til at hashe passwordet med salt
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8; // Antallet af CPU kerner (tilpas efter behov)
                argon2.MemorySize = 65536; // Størrelsen af hukommelsen i kilobytes
                argon2.Iterations = 4; // Antallet af iterationer

                // Generer hash
                var hash = argon2.GetBytes(32); // Generér hash på 32 bytes

                // Kombinér salt og hash til at gemme dem sammen i databasen
                byte[] hashBytes = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                return Convert.ToBase64String(hashBytes); // Returner som en Base64-streng
            }
        }

        public async Task<bool> RegisterUser(string username, string password, string role)
        {
            // Check if the username already exists
            var checkQuery = "SELECT COUNT(*) FROM users WHERE Username = @Username";
            var parameters = new[] { new SqlParameter("@Username", username) };

            var result = await _dConnection.ExecuteQueryAsync(checkQuery, parameters);
            if (result.Rows[0][0].ToString() != "0")
            {
                throw new Exception("Username is already taken.");
            }

            // Hash the password and create the new user
            var hashedPassword = HashPassword(password);
            var insertQuery = "INSERT INTO users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role)";
            var insertParams = new[]
            {
                new SqlParameter("@Username", username),
                new SqlParameter("@PasswordHash", hashedPassword),
                new SqlParameter("@Role", role)
            };

            await _dConnection.ExecuteNonQueryAsync(insertQuery, insertParams);
            return true;
        }

        public async Task<User> Login(string username, string password)
        {
            var query = "SELECT Id, Username, PasswordHash, Role FROM users WHERE Username = @Username";
            var parameters = new[] { new SqlParameter("@Username", username) };

            var result = await _dConnection.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                throw new Exception("User not found.");

            var user = result.Rows[0];
            var storedHashWithSalt = Convert.FromBase64String(user["PasswordHash"].ToString());

            // Split salt and hash
            var salt = new byte[16]; // Saltens længde
            Buffer.BlockCopy(storedHashWithSalt, 0, salt, 0, salt.Length);

            var storedHash = new byte[storedHashWithSalt.Length - salt.Length];
            Buffer.BlockCopy(storedHashWithSalt, salt.Length, storedHash, 0, storedHash.Length);

            // Hash the incoming password with the salt
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;

                var passwordHash = argon2.GetBytes(32);

                // Compare the generated hash with the stored hash
                if (!passwordHash.SequenceEqual(storedHash))
                    throw new Exception("Incorrect password.");

                return new User
                {
                    Id = Convert.ToInt32(user["Id"]),
                    Username = user["Username"].ToString(),
                    Role = user["Role"].ToString()
                };
            }
        }
    }
}
