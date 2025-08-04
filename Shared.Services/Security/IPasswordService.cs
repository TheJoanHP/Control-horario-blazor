namespace Shared.Services.Security
{
    /// <summary>
    /// Interfaz para el servicio de gestión de contraseñas
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Genera un hash de contraseña usando BCrypt
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Hash de la contraseña</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifica si una contraseña coincide con su hash
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="hashedPassword">Hash de la contraseña</param>
        /// <returns>True si coincide, false si no</returns>
        bool VerifyPassword(string password, string hashedPassword);

        /// <summary>
        /// Genera una contraseña aleatoria segura
        /// </summary>
        /// <param name="length">Longitud de la contraseña (por defecto 12)</param>
        /// <param name="includeSpecialChars">Incluir caracteres especiales</param>
        /// <returns>Contraseña generada</returns>
        string GenerateRandomPassword(int length = 12, bool includeSpecialChars = true);

        /// <summary>
        /// Evalúa la fortaleza de una contraseña
        /// </summary>
        /// <param name="password">Contraseña a evaluar</param>
        /// <returns>Score de 0-100 indicando la fortaleza</returns>
        int EvaluatePasswordStrength(string password);

        /// <summary>
        /// Verifica si una contraseña cumple con los requisitos mínimos de seguridad
        /// </summary>
        /// <param name="password">Contraseña a verificar</param>
        /// <returns>True si es válida, false si no</returns>
        bool IsPasswordValid(string password);
    }
}