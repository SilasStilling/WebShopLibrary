using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using WebShopLibrary.Database;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace WebShopLibrary
{
    public class LogService
    {
        private readonly LogDBConnection _logDBConnection; 
        public LogService(LogDBConnection logDBConnection)
        {
            _logDBConnection = logDBConnection;
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public async Task LogAsync(string action, string? username, string result, string? error = null)
        {
            var log = new
            {
                Timestamp = DateTime.UtcNow,
                Username = username ?? "Anonymous",
                Action = action,
                Result = result,
                Error = error
            };

            var json = JsonSerializer.Serialize(log);
            var hash = ComputeSha256Hash(json);

            var query = "INSERT INTO Logs (LogEntry, Hash) VALUES (@LogEntry, @Hash)";
            var parameters = new[]
            {
               new SqlParameter("@LogEntry", json),
               new SqlParameter("@Hash", hash)
            };
            await _logDBConnection.ExecuteNonQueryAsync(query, parameters);
        }
    }
}
