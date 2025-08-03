using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Shared.Services.Tenant
{
    public interface ITenantResolver
    {
        string GetTenantId();
        int GetCompanyId();
        string GetConnectionString();
    }

    public class TenantResolver : ITenantResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public TenantResolver(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string GetTenantId()
        {
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
                if (parts.Length > 2)
                {
                    var subdomain = parts[0];
                    if (subdomain != "www" && subdomain != "api")
                        return subdomain;
                }
            }

            // Por defecto devolver "default" para desarrollo
            return "default";
        }

        public int GetCompanyId()
        {
            var context = _httpContextAccessor.HttpContext;
            
            // Intentar obtener del claim del JWT
            var claimsPrincipal = context?.User;
            if (claimsPrincipal?.Identity?.IsAuthenticated == true)
            {
                var companyClaim = claimsPrincipal.FindFirst("company_id");
                if (companyClaim != null && int.TryParse(companyClaim.Value, out int companyId))
                    return companyId;
            }

            // Si no hay company_id en los claims, lanzar excepción
            throw new InvalidOperationException("No se pudo determinar el Company ID del usuario actual");
        }

        public string GetConnectionString()
        {
            var tenantId = GetTenantId();
            
            // Para desarrollo, usar la misma conexión
            if (tenantId == "default")
            {
                return _configuration.GetConnectionString("DefaultConnection") 
                    ?? throw new InvalidOperationException("ConnectionString no configurado");
            }

            // En producción, cada tenant tendría su propia BD
            var tenantConnectionString = _configuration.GetConnectionString($"Tenant_{tenantId}");
            
            // Si no existe conexión específica, usar la por defecto
            return tenantConnectionString 
                ?? _configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("ConnectionString no configurado");
        }
    }
}