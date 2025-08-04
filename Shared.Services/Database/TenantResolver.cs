using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Shared.Services.Database  // ← Cambié el namespace para ser consistente
{
    public class TenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private string? _currentTenantId;

        public TenantResolver(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string GetTenantId()
        {
            // Si ya se estableció manualmente (por middleware), usar ese
            if (!string.IsNullOrEmpty(_currentTenantId))
                return _currentTenantId;

            var context = _httpContextAccessor.HttpContext;
            
            // Intentar obtener del header
            if (context?.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) == true)
            {
                var tenantId = tenantHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(tenantId))
                    return tenantId;
            }

            // Intentar obtener del claim del JWT
            var claimsPrincipal = context?.User;
            if (claimsPrincipal?.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = claimsPrincipal.FindFirst("tenant_id");
                if (tenantClaim != null && !string.IsNullOrEmpty(tenantClaim.Value))
                    return tenantClaim.Value;
            }

            // Intentar obtener del host/subdomain
            var host = context?.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                // Si es un subdominio como empresa1.tudominio.com
                var parts = host.Split('.');
                if (parts.Length > 2 && !string.Equals(parts[0], "www", StringComparison.OrdinalIgnoreCase))
                {
                    return parts[0];
                }
            }

            // Por defecto en desarrollo
            return "demo";
        }

        public int GetCompanyId()
        {
            // Obtener company_id del JWT claim
            var context = _httpContextAccessor.HttpContext;
            var claimsPrincipal = context?.User;
            
            if (claimsPrincipal?.Identity?.IsAuthenticated == true)
            {
                var companyClaim = claimsPrincipal.FindFirst("company_id");
                if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
                    return companyId;
            }

            // Por defecto devolver 1 (primera empresa)
            return 1;
        }

        public bool HasTenant()
        {
            return !string.IsNullOrEmpty(GetTenantId());
        }

        public string GetConnectionString()
        {
            var tenantId = GetTenantId();
            
            // Construir string de conexión específico del tenant
            var baseConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            
            // Reemplazar el nombre de la base de datos con el tenant
            if (baseConnectionString.Contains("Database="))
            {
                var parts = baseConnectionString.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    {
                        parts[i] = $"Database=SphereTime_{tenantId}";
                        break;
                    }
                }
                return string.Join(";", parts);
            }
            
            // Si no encuentra Database=, agregar el nombre de BD
            return baseConnectionString + $";Database=SphereTime_{tenantId}";
        }

        public void SetTenantId(string tenantId)
        {
            _currentTenantId = tenantId;
        }

        public void SetTenant(string tenantId)
        {
            _currentTenantId = tenantId;
        }
    }
}