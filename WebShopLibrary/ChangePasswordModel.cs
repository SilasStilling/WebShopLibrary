using Konscious.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebShopLibrary
{
    public class ChangePasswordModel
    {
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        private string HashPassword(string password)
        {
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = GenerateSalt();
                argon2.DegreeOfParallelism = 8;
                argon2.MemorySize = 65536;
                argon2.Iterations = 4;

                var hash = argon2.GetBytes(32);
                var hashBytes = new byte[16 + hash.Length];
                Buffer.BlockCopy(argon2.Salt, 0, hashBytes, 0, 16);
                Buffer.BlockCopy(hash, 0, hashBytes, 16, hash.Length);

                return Convert.ToBase64String(hashBytes);
            }
        }
        private byte[] GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}
