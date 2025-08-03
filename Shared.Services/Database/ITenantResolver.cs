namespace Shared.Services.Database
{
    public interface ITenantResolver
    {
        /// <summary>
        /// Obtiene el ID del tenant actual
        /// </summary>
        /// <returns>ID del tenant</returns>
        string GetTenantId();

        /// <summary>
        /// Establece el ID del tenant actual
        /// </summary>
        /// <param name="tenantId">ID del tenant</param>
        void SetTenantId(string tenantId);

        /// <summary>
        /// Obtiene la cadena de conexión para el tenant actual
        /// </summary>
        /// <returns>Cadena de conexión</returns>
        string GetConnectionString();

        /// <summary>
        /// Verifica si hay un tenant configurado
        /// </summary>
        /// <returns>True si hay un tenant</returns>
        bool HasTenant();
    }
}