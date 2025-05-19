using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;
using WebShopLibrary.Database;

namespace WebShopLibrary
{
    public class AuthService
    {
        private readonly DBConnection _dConnection;
        private readonly LogService _logService;

        private static Dictionary<string, int> loginAttempts = new Dictionary<string, int>();
        private static Dictionary<string, DateTime> lockoutTime = new Dictionary<string, DateTime>();
        private const int maxAttempts = 5;
        private const int lockoutDurationMinutes = 5;

        public AuthService(DBConnection dbConnection, LogService logService)
        {
            _dConnection = dbConnection;
            _logService = logService;
        }

        public string HashPassword(string password)
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
                byte[] hashBytes = new byte[salt.Length + hash.Length];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
                Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

                return Convert.ToBase64String(hashBytes);
            }
        }

        public async Task<bool> RegisterUser(string username, string password, string role)
        {
            try
            {
                var checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                var parameters = new[] { new SqlParameter("@Username", username) };
                var result = await _dConnection.ExecuteQueryAsync(checkQuery, parameters);

                if (result.Rows[0][0].ToString() != "0")
                {
                    await _logService.LogAsync("Register", username, "Failed", "Username already taken");
                    throw new Exception("Username is already taken.");
                }

                var hashedPassword = HashPassword(password);
                var insertQuery = "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role)";
                var insertParams = new[]
                {
                    new SqlParameter("@Username", username),
                    new SqlParameter("@PasswordHash", hashedPassword),
                    new SqlParameter("@Role", role)
                };

                await _dConnection.ExecuteNonQueryAsync(insertQuery, insertParams);
                await _logService.LogAsync("Register", username, "Success");
                return true;
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Register", username, "Failed", ex.Message);
                throw;
            }
        }

        public async Task<User> Login(string username, string password)
        {
            if (lockoutTime.ContainsKey(username) && DateTime.UtcNow < lockoutTime[username])
            {
                await _logService.LogAsync("Login", username, "Failed", "Account locked");
                throw new Exception($"Account locked. Try again in {(lockoutTime[username] - DateTime.UtcNow).Minutes} minutes.");
            }

            var query = "SELECT Id, Username, PasswordHash, Role FROM Users WHERE Username = @Username";
            var parameters = new[] { new SqlParameter("@Username", username) };
            var result = await _dConnection.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                RegisterLoginAttempt(username, false);
                await _logService.LogAsync("Login", username, "Failed", "User not found");
                throw new Exception("User not found.");
            }

            var user = result.Rows[0];
            var passwordHashString = user["PasswordHash"].ToString();

            if (string.IsNullOrEmpty(passwordHashString))
            {
                RegisterLoginAttempt(username, false);
                await _logService.LogAsync("Login", username, "Failed", "Password hash is null");
                throw new Exception("Password hash is null.");
            }

            var storedHashWithSalt = Convert.FromBase64String(passwordHashString);
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

                if (!passwordHash.SequenceEqual(storedHash))
                {
                    RegisterLoginAttempt(username, false);
                    await _logService.LogAsync("Login", username, "Failed", "Incorrect password");
                    throw new Exception($"Incorrect password. {maxAttempts - loginAttempts[username]} attempts left.");
                }
            }

            RegisterLoginAttempt(username, true);
            await _logService.LogAsync("Login", username, "Success");

            return new User
            {
                Id = Convert.ToInt32(user["Id"]),
                Username = user["Username"].ToString(),
                Role = user["Role"].ToString()
            };
        }

        private void RegisterLoginAttempt(string username, bool success)
        {
            if (success)
            {
                loginAttempts[username] = 0;
                return;
            }

            if (!loginAttempts.ContainsKey(username))
                loginAttempts[username] = 0;

            loginAttempts[username]++;

            if (loginAttempts[username] >= maxAttempts)
                lockoutTime[username] = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
        }
    }
}
