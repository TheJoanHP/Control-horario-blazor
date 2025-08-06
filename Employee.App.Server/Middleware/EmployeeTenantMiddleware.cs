using Shared.Services.Database;

namespace Employee.App.Server.Middleware
{
    public class EmployeeTenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EmployeeTenantMiddleware> _logger;

        public EmployeeTenantMiddleware(RequestDelegate next, ILogger<EmployeeTenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
        {
            try
            {
                // Resolver el tenant de la misma manera que en Company.Admin
                var tenantId = ResolveTenantId(context);
                
                if (string.IsNullOrEmpty(tenantId))
                {
                    // Para empleados, es más flexible - permitir tenant por defecto
                    _logger.LogWarning("No se pudo resolver el tenant para empleado: {Host}", context.Request.Host);
                    tenantId = "demo"; // Tenant por defecto
                }

                // Configurar el tenant en el resolver
                tenantResolver.SetTenant(tenantId);
                
                _logger.LogInformation("Tenant resuelto para empleado: {TenantId} en host: {Host}", tenantId, context.Request.Host);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resolver el tenant para empleado");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Error de configuración de tenant");
                return;
            }

            await _next(context);
        }

        private string ResolveTenantId(HttpContext context)
        {
            // Método 1: Por subdominio
            var host = context.Request.Host.Host;
            var parts = host.Split('.');
            
            if (parts.Length > 2 && !parts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
            {
                return parts[0];
            }

            // Método 2: Por header personalizado (para apps móviles)
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenant))
            {
                return headerTenant.ToString();
            }

            // Método 3: Por query parameter
            if (context.Request.Query.TryGetValue("tenant", out var queryTenant))
            {
                return queryTenant.ToString();
            }

            // Método 4: Para desarrollo local
            if (host.StartsWith("localhost") || host.StartsWith("127.0.0.1"))
            {
                return context.Request.Query["tenant"].FirstOrDefault() 
                    ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                    ?? "demo";
            }

            // Método 5: Desde JWT token si está presente
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                try
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    // Aquí podrías extraer el tenant del JWT token
                    // Por simplicidad, no lo implementamos aquí
                }
                catch
                {
                    // Ignorar errores de parsing del token
                }
            }

            return string.Empty;
        }
    }
}