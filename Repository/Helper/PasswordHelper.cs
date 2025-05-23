﻿using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Repository.Helper
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            try
            {
                byte[] salt = new byte[128 / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

                return $"{Convert.ToBase64String(salt)}:{hashed}";
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while hashing the password.", ex);
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHashWithSalt)
        {
            try
            {
                var parts = storedHashWithSalt.Split(':');
                if (parts.Length != 2) return false;

                var salt = Convert.FromBase64String(parts[0]);
                var storedHash = parts[1];

                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: enteredPassword,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

                return hashed == storedHash;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while verifying the password.", ex);
            }
        }
    }
}
