using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Services.Security
{
    /// <summary>
    /// Implementación del servicio de gestión de contraseñas
    /// </summary>
    public class PasswordService : IPasswordService
    {
        private const int DefaultWorkFactor = 12;
        private const string PasswordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, DefaultWorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public string GenerateRandomPassword(int length = 12, bool includeSpecialChars = true)
        {
            if (length < 6)
                throw new ArgumentException("La longitud mínima es 6 caracteres", nameof(length));

            var chars = PasswordChars;
            if (includeSpecialChars)
                chars += SpecialChars;

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            var result = new StringBuilder(length);
            foreach (byte b in bytes)
            {
                result.Append(chars[b % chars.Length]);
            }

            var password = result.ToString();

            // Asegurar que tiene al menos una minúscula, mayúscula y número
            if (!HasLowerCase(password))
                password = ReplaceRandomChar(password, GetRandomChar("abcdefghijklmnopqrstuvwxyz"));
            
            if (!HasUpperCase(password))
                password = ReplaceRandomChar(password, GetRandomChar("ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
            
            if (!HasDigit(password))
                password = ReplaceRandomChar(password, GetRandomChar("0123456789"));

            if (includeSpecialChars && !HasSpecialChar(password))
                password = ReplaceRandomChar(password, GetRandomChar(SpecialChars));

            return password;
        }

        public int EvaluatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;

            // Longitud
            if (password.Length >= 8) score += 25;
            else if (password.Length >= 6) score += 10;

            // Tiene minúsculas
            if (HasLowerCase(password)) score += 15;

            // Tiene mayúsculas  
            if (HasUpperCase(password)) score += 15;

            // Tiene números
            if (HasDigit(password)) score += 15;

            // Tiene caracteres especiales
            if (HasSpecialChar(password)) score += 15;

            // Longitud extra (bonus)
            if (password.Length >= 12) score += 10;
            if (password.Length >= 16) score += 5;

            // Penalizar patrones comunes
            if (HasRepeatingChars(password)) score -= 10;
            if (HasSequentialChars(password)) score -= 10;

            return Math.Max(0, Math.Min(100, score));
        }

        public bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Requisitos mínimos
            return password.Length >= 6 &&
                   HasLowerCase(password) &&
                   HasUpperCase(password) &&
                   HasDigit(password);
        }

        private bool HasLowerCase(string password) => password.Any(char.IsLower);
        private bool HasUpperCase(string password) => password.Any(char.IsUpper);
        private bool HasDigit(string password) => password.Any(char.IsDigit);
        private bool HasSpecialChar(string password) => password.Any(c => SpecialChars.Contains(c));

        private bool HasRepeatingChars(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                    return true;
            }
            return false;
        }

        private bool HasSequentialChars(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] + 1 == password[i + 1] && password[i + 1] + 1 == password[i + 2])
                    return true;
                if (password[i] - 1 == password[i + 1] && password[i + 1] - 1 == password[i + 2])
                    return true;
            }
            return false;
        }

        private char GetRandomChar(string chars)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[1];
            rng.GetBytes(bytes);
            return chars[bytes[0] % chars.Length];
        }

        private string ReplaceRandomChar(string password, char newChar)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[1];
            rng.GetBytes(bytes);
            var index = bytes[0] % password.Length;
            
            var chars = password.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }
    }
}