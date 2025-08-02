using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Shared.Services.Security
{
    public class PasswordService : IPasswordService
    {
        private const int WorkFactor = 12;

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
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

        public string GenerateRandomPassword(int length = 12)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            var password = new List<char>();

            // Asegurar al menos un carácter de cada tipo
            password.Add(upperCase[random.Next(upperCase.Length)]);
            password.Add(lowerCase[random.Next(lowerCase.Length)]);
            password.Add(digits[random.Next(digits.Length)]);
            password.Add(specialChars[random.Next(specialChars.Length)]);

            // Completar el resto de la longitud
            var allChars = upperCase + lowerCase + digits + specialChars;
            for (int i = 4; i < length; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Mezclar los caracteres
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        public bool IsPasswordStrong(string password)
        {
            var (isValid, _) = ValidatePasswordStrength(password);
            return isValid;
        }

        public (bool IsValid, List<string> Errors) ValidatePasswordStrength(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("La contraseña es requerida");
                return (false, errors);
            }

            if (password.Length < 8)
                errors.Add("La contraseña debe tener al menos 8 caracteres");

            if (password.Length > 128)
                errors.Add("La contraseña no puede exceder 128 caracteres");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("La contraseña debe contener al menos una letra mayúscula");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("La contraseña debe contener al menos una letra minúscula");

            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("La contraseña debe contener al menos un número");

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]"))
                errors.Add("La contraseña debe contener al menos un carácter especial");

            // Verificar patrones comunes débiles
            var commonPatterns = new[]
            {
                @"(.)\1{2,}", // Caracteres repetidos consecutivos
                @"123456", @"654321", // Secuencias numéricas
                @"abcdef", @"fedcba", // Secuencias alfabéticas
                @"qwerty", @"asdfgh", // Patrones de teclado
                @"password", @"admin", @"user" // Palabras comunes
            };

            foreach (var pattern in commonPatterns)
            {
                if (Regex.IsMatch(password.ToLower(), pattern))
                {
                    errors.Add("La contraseña contiene patrones comunes que la hacen débil");
                    break;
                }
            }

            return (errors.Count == 0, errors);
        }
    }
}