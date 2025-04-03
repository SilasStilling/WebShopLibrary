using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace WebShopLibrary
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }


        public override string ToString()
        {
            return $"User: {Username}, Email: {Email}";
        }

        public void ValidateName()
        {
            if (Username == null)
            {
                throw new ArgumentNullException(nameof(Username), "Name cannot be null");
            }
            if (Username.Length <= 2)
            {
                throw new ArgumentOutOfRangeException("Name must be at least 2 characters long", nameof(Username));
            }

        }

        public void ValidateEmail()
        {
            if (string.IsNullOrEmpty(Email))
            {
                throw new ArgumentNullException(nameof(Email), "Email is required");
            }

            try
            {
                var addr = new MailAddress(Email);
                if (addr.Address != Email)
                {
                    throw new ArgumentException("Email is not valid!");
                }
            }
            catch
            {
                throw new ArgumentException("Email is not valid!");
            }
        }

        public void ValidatePassword()
        {
            if (Password == null)
            {
                throw new ArgumentNullException(nameof(Password), "Password cannot be null");
            }
            if (Password.Length < 12 || Password.Length > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(Password), "Password must be between 12 and 256 characters long");
            }
            if (Password.Contains(" "))
            {
                throw new ArgumentException("Password cannot contain spaces", nameof(Password));
            }
            if (Regex.Matches(Password, "[A-Z]").Count < 2)
            {
                throw new ArgumentException("Password must contain at least 2 uppercase letters", nameof(Password));
            }
            if (Regex.Matches(Password, "[a-z]").Count < 2)
            {
                throw new ArgumentException("Password must contain at least 2 lowercase letters", nameof(Password));
            }
            if (!Regex.IsMatch(Password, @"[\W_]"))
            {
                throw new ArgumentException("Password must contain at least 1 special character", nameof(Password));
            }
            if (Username != null && Password.Contains(Username))
            {
                throw new ArgumentException("Password cannot contain the username", nameof(Password));
            }
            if (Regex.IsMatch(Password, @"(\d)\1{2,}"))
            {
                throw new ArgumentException("Password cannot contain repeated sequences", nameof(Password));
            }
            if (Regex.IsMatch(Password, @"\d{4,}"))
            {
                throw new ArgumentException("Password cannot contain sequences of numbers like 1234", nameof(Password));
            }
        }
        public void Validate()
        {
            ValidateName();
            ValidateEmail();
            ValidatePassword();
        }
    }
}
