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
        /// Obtiene el ID de la empresa del usuario actual
        /// </summary>
        /// <returns>ID de la empresa</returns>
        int GetCompanyId();

        /// <summary>
        /// Obtiene la cadena de conexión para el tenant actual
        /// </summary>
        /// <returns>Cadena de conexión</returns>
        string GetConnectionString();

        /// <summary>
        /// Establece el ID del tenant actual
        /// </summary>
        /// <param name="tenantId">ID del tenant</param>
        void SetTenantId(string tenantId);

        /// <summary>
        /// Establece el tenant actual (método alternativo)
        /// </summary>
        /// <param name="tenantId">ID del tenant</param>
        void SetTenant(string tenantId);

        /// <summary>
        /// Verifica si hay un tenant configurado
        /// </summary>
        /// <returns>True si hay un tenant</returns>
        bool HasTenant();
    }
}