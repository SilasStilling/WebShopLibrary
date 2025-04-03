using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using WebShopLibrary.Database;

namespace WebShopLibrary
{
    public class AuthService
    {
        private readonly DBConnection _dConnection;
        
        private static Dictionary<string, int> loginAttempts = new Dictionary<string, int>();
        private static Dictionary<string, DateTime> lockoutTime = new Dictionary<string, DateTime>();
        private const int maxAttempts = 5;
        private const int lockoutDurationMinutes = 5; // Lås konto i 5 minutter


        public AuthService(DBConnection dbConnection)
        {
            _dConnection = dbConnection;
        }

        // Hashes password with Argon2

        // husk skal være private
        public string HashPassword(string password)
        {
            var salt = new byte[16]; // Generate a random salt
            RandomNumberGenerator.Fill(salt); // Fill salt array with random bytes

            // Use Argon2 to hash the password with salt
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8; // Number of CPU cores (adjust as needed)
                argon2.MemorySize = 65536; // Memory size in kilobytes
                argon2.Iterations = 4; // Number of iterations

                // Generate hash
                var hash = argon2.GetBytes(32); // Generate 32-byte hash

                // Combine salt and hash to store them together in the database
                byte[] hashBytes = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                return Convert.ToBase64String(hashBytes); // Return as a Base64 string
            }
        }

        public async Task<bool> RegisterUser(string username, string password, string role)
        {
            // Check if the username already exists
            var checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            var parameters = new[] { new SqlParameter("@Username", username) };

            var result = await _dConnection.ExecuteQueryAsync(checkQuery, parameters);
            if (result.Rows[0][0].ToString() != "0")
            {
                throw new Exception("Username is already taken.");
            }

            // Hash the password and create the new user
            var hashedPassword = HashPassword(password);
            var insertQuery = "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role)";
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
            // Tjek om brugeren er låst ude
            if (lockoutTime.ContainsKey(username) && DateTime.Now < lockoutTime[username])
            {
                throw new Exception($"Account locked. Try again in {lockoutTime[username] - DateTime.Now:mm} minutes.");
            }

            var query = "SELECT Id, Username, PasswordHash, Role FROM Users WHERE Username = @Username";
            var parameters = new[] { new SqlParameter("@Username", username) };

            var result = await _dConnection.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                throw new Exception("User not found.");

            var user = result.Rows[0];
            var passwordHashString = user["PasswordHash"].ToString();
            if (passwordHashString == null)
                throw new Exception("Password hash is null.");

            var storedHashWithSalt = Convert.FromBase64String(passwordHashString);

            // Split salt og hash
            var salt = new byte[16];
            Buffer.BlockCopy(storedHashWithSalt, 0, salt, 0, salt.Length);

            var storedHash = new byte[storedHashWithSalt.Length - salt.Length];
            Buffer.BlockCopy(storedHashWithSalt, salt.Length, storedHash, 0, storedHash.Length);

            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = 8;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;

                var passwordHash = argon2.GetBytes(32);

                // Hvis adgangskoden er forkert
                if (!passwordHash.SequenceEqual(storedHash))
                {
                    if (!loginAttempts.ContainsKey(username))
                        loginAttempts[username] = 0;

                    loginAttempts[username]++;

                    if (loginAttempts[username] >= maxAttempts)
                    {
                        lockoutTime[username] = DateTime.Now.AddMinutes(lockoutDurationMinutes);
                        throw new Exception("Too many failed attempts. Your account is locked for 5 minutes.");
                    }

                    throw new Exception($"Incorrect password. {maxAttempts - loginAttempts[username]} attempts left.");
                }

                // Login er korrekt, nulstil fejlforsøg
                loginAttempts[username] = 0;

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