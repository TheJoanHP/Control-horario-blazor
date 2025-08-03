using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Shared.Services.Security
{
    public class PasswordService : IPasswordService
    {
        private readonly int _workFactor;
        private readonly string _allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

        public PasswordService(int workFactor = 12)
        {
            _workFactor = workFactor;
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Mínimo 6 caracteres
            if (password.Length < 6)
                return false;

            // Al menos una letra
            if (!Regex.IsMatch(password, @"[a-zA-Z]"))
                return false;

            // Al menos un número
            if (!Regex.IsMatch(password, @"[0-9]"))
                return false;

            // Opcional: al menos un carácter especial (comentado por flexibilidad)
            // if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            //     return false;

            return true;
        }

        public string GenerateTemporaryPassword(int length = 12)
        {
            if (length < 6)
                throw new ArgumentException("La longitud mínima es 6 caracteres", nameof(length));

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(_allowedChars[bytes[i] % _allowedChars.Length]);
            }

            var password = result.ToString();

            // Asegurar que tenga al menos una letra y un número
            if (!IsValidPassword(password))
            {
                // Reemplazar los primeros caracteres para garantizar los requisitos
                var chars = password.ToCharArray();
                chars[0] = 'A'; // Asegurar una letra mayúscula
                chars[1] = '1'; // Asegurar un número
                password = new string(chars);
            }

            return password;
        }
    }
}