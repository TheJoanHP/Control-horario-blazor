using Shared.Services.Database;

namespace Company.Admin.Server.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
        {
            var tenantId = ResolveTenantId(context);
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                tenantResolver.SetTenantId(tenantId);
                _logger.LogDebug("Tenant configurado: {TenantId}", tenantId);
            }
            else
            {
                // En desarrollo, usar un tenant por defecto
                tenantResolver.SetTenantId("demo");
                _logger.LogDebug("Usando tenant por defecto: demo");
            }

            await _next(context);
        }

        private string? ResolveTenantId(HttpContext context)
        {
            // 1. Intentar desde el subdominio
            var host = context.Request.Host.Host;
            if (!string.IsNullOrEmpty(host) && host != "localhost")
            {
                var parts = host.Split('.');
                if (parts.Length > 2) // ej: empresa1.tudominio.com
                {
                    return parts[0];
                }
            }

            // 2. Intentar desde el header X-Tenant-ID
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader))
            {
                return tenantHeader.FirstOrDefault();
            }

            // 3. Intentar desde query parameter
            if (context.Request.Query.TryGetValue("tenant", out var tenantQuery))
            {
                return tenantQuery.FirstOrDefault();
            }

            // 4. Intentar desde el path (ej: /api/tenant/empresa1/...)
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 2 && segments[0] == "api" && segments[1] == "tenant")
                {
                    return segments[2];
                }
            }

            return null;
        }
    }
}