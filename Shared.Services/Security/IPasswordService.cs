namespace Shared.Services.Security
{
    public interface IPasswordService
    {
        /// <summary>
        /// Hashea una contraseña usando BCrypt
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Hash de la contraseña</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifica si una contraseña coincide con su hash
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="hash">Hash almacenado</param>
        /// <returns>True si la contraseña es correcta</returns>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Valida si una contraseña cumple con los requisitos de seguridad
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>True si es válida</returns>
        bool IsValidPassword(string password);

        /// <summary>
        /// Genera una contraseña temporal aleatoria
        /// </summary>
        /// <param name="length">Longitud de la contraseña</param>
        /// <returns>Contraseña temporal</returns>
        string GenerateTemporaryPassword(int length = 12);
    }
}