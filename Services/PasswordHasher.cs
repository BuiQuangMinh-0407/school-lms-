using System.Security.Cryptography;

namespace SchoolManagement.Services
{
    public static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        /// <summary>
        /// Hashes a password using PBKDF2 with HMAC-SHA256 (matches DBNew2026 format).
        /// Format: PBKDF2${iterations}${salt_base64}${key_base64}
        /// </summary>
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        /// <summary>
        /// Verifies a password against PBKDF2 or legacy BCrypt hash.
        /// </summary>
        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            // 1. Check PBKDF2 format (used in DBNew2026)
            if (storedHash.StartsWith("PBKDF2$", StringComparison.OrdinalIgnoreCase))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4) return false;
                if (!int.TryParse(parts[1], out int iterations)) return false;

                try
                {
                    byte[] salt = Convert.FromBase64String(parts[2]);
                    byte[] expectedKey = Convert.FromBase64String(parts[3]);

                    byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(
                        password,
                        salt,
                        iterations,
                        HashAlgorithmName.SHA256,
                        expectedKey.Length);

                    return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
                }
                catch
                {
                    return false;
                }
            }

            // 2. Fallback to BCrypt if any
            if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$") || storedHash.StartsWith("$2y$"))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
                }
                catch
                {
                    return false;
                }
            }

            // 3. Fallback plaintext comparison (for safety in dev)
            return password == storedHash;
        }
    }
}
