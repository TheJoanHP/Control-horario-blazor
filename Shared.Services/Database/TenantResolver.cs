using Microsoft.Extensions.Configuration;

namespace Shared.Services.Database
{
    public class TenantResolver : ITenantResolver
    {
        private readonly IConfiguration _configuration;
        private string? _tenantId;

        public TenantResolver(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetTenantId()
        {
            return _tenantId ?? "default";
        }

        public void SetTenantId(string tenantId)
        {
            _tenantId = tenantId;
        }

        public string GetConnectionString()
        {
            var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(baseConnectionString))
            {
                throw new InvalidOperationException("No se encontró la cadena de conexión por defecto");
            }

            // En un entorno real, aquí modificarías la cadena de conexión para incluir la base de datos del tenant
            // Por ahora, usamos la misma base de datos pero con el tenant identificado
            return baseConnectionString;
        }

        public bool HasTenant()
        {
            return !string.IsNullOrEmpty(_tenantId);
        }
    }
}