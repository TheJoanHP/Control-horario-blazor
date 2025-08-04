using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.Enums;
using Sphere.Admin.Server.Data;
using Database.Setup.Tools;

namespace Sphere.Admin.Server.Controllers
{
    /// <summary>
    /// Controlador para gestión de tenants/empresas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly SphereDbContext _context;
        private readonly ILogger<TenantsController> _logger;
        private readonly IConfiguration _configuration;

        public TenantsController(
            SphereDbContext context,
            ILogger<TenantsController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Obtener todos los tenants
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants()
        {
            try
            {
                var tenants = await _context.Tenants
                    .Include(t => t.License)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tenants");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener un tenant por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenant(int id)
        {
            try
            {
                var tenant = await _context.Tenants
                    .Include(t => t.License)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                {
                    return NotFound(new { message = "Tenant no encontrado" });
                }

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo tenant {TenantId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crear un nuevo tenant
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verificar que el código y subdominio no existan
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Code == request.Code || t.Subdomain == request.Subdomain);

                if (existingTenant != null)
                {
                    return BadRequest(new { message = "El código o subdominio ya existe" });
                }

                // Crear nuevo tenant
                var tenant = new Tenant
                {
                    Code = request.Code,
                    Name = request.Name,
                    Description = request.Description,
                    Subdomain = request.Subdomain,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    DatabaseName = $"SphereTimeControl_{request.Code}",
                    Active = true,
                    LicenseType = request.LicenseType ?? LicenseType.Trial,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                // Crear licencia por defecto
                var license = new License
                {
                    TenantId = tenant.Id,
                    LicenseType = tenant.LicenseType,
                    MaxEmployees = GetMaxEmployeesForLicense(tenant.LicenseType),
                    HasReports = tenant.LicenseType != LicenseType.Trial,
                    HasAPI = tenant.LicenseType == LicenseType.Professional || tenant.LicenseType == LicenseType.Enterprise,
                    HasMobileApp = true,
                    MonthlyPrice = GetPriceForLicense(tenant.LicenseType),
                    StartDate = DateTime.UtcNow,
                    EndDate = tenant.LicenseType == LicenseType.Trial 
                        ? DateTime.UtcNow.AddDays(30) 
                        : DateTime.UtcNow.AddMonths(1),
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Licenses.Add(license);
                await _context.SaveChangesAsync();

                // Crear base de datos del tenant usando TenantCreator
                await CreateTenantDatabaseAsync(tenant.Code, tenant.Name, tenant.ContactEmail);

                // Recargar tenant con licencia
                await _context.Entry(tenant).Reference(t => t.License).LoadAsync();

                _logger.LogInformation("Tenant creado exitosamente: {TenantCode}", tenant.Code);
                return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando tenant");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Actualizar un tenant
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Tenant>> UpdateTenant(int id, [FromBody] UpdateTenantRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tenant = await _context.Tenants
                    .Include(t => t.License)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                {
                    return NotFound(new { message = "Tenant no encontrado" });
                }

                // Verificar que el código y subdominio no existan en otros tenants
                var existingTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id != id && (t.Code == request.Code || t.Subdomain == request.Subdomain));

                if (existingTenant != null)
                {
                    return BadRequest(new { message = "El código o subdominio ya existe en otro tenant" });
                }

                // Actualizar campos
                tenant.Code = request.Code;
                tenant.Name = request.Name;
                tenant.Description = request.Description;
                tenant.Subdomain = request.Subdomain;
                tenant.ContactEmail = request.ContactEmail;
                tenant.ContactPhone = request.ContactPhone;
                tenant.Active = request.Active;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant actualizado: {TenantCode}", tenant.Code);
                return Ok(tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando tenant {TenantId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Eliminar un tenant
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTenant(int id)
        {
            try
            {
                var tenant = await _context.Tenants
                    .Include(t => t.License)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                {
                    return NotFound(new { message = "Tenant no encontrado" });
                }

                // Eliminar base de datos del tenant
                await DeleteTenantDatabaseAsync(tenant.Code);

                // Eliminar licencia primero (por foreign key)
                if (tenant.License != null)
                {
                    _context.Licenses.Remove(tenant.License);
                }

                // Eliminar tenant
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant eliminado: {TenantCode}", tenant.Code);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando tenant {TenantId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Activar/Desactivar un tenant
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<Tenant>> ToggleTenantStatus(int id)
        {
            try
            {
                var tenant = await _context.Tenants.FindAsync(id);

                if (tenant == null)
                {
                    return NotFound(new { message = "Tenant no encontrado" });
                }

                tenant.Active = !tenant.Active;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Estado del tenant cambiado: {TenantCode} -> {Status}", 
                    tenant.Code, tenant.Active ? "Activo" : "Inactivo");

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando estado del tenant {TenantId}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de tenants
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetTenantStats()
        {
            try
            {
                var stats = new
                {
                    TotalTenants = await _context.Tenants.CountAsync(),
                    ActiveTenants = await _context.Tenants.CountAsync(t => t.Active),
                    InactiveTenants = await _context.Tenants.CountAsync(t => !t.Active),
                    TrialTenants = await _context.Tenants.CountAsync(t => t.LicenseType == LicenseType.Trial),
                    BasicTenants = await _context.Tenants.CountAsync(t => t.LicenseType == LicenseType.Basic),
                    ProfessionalTenants = await _context.Tenants.CountAsync(t => t.LicenseType == LicenseType.Professional),
                    EnterpriseTenants = await _context.Tenants.CountAsync(t => t.LicenseType == LicenseType.Enterprise),
                    ExpiredLicenses = await _context.Licenses.CountAsync(l => l.EndDate < DateTime.UtcNow && l.Active),
                    ExpiringLicenses = await _context.Licenses.CountAsync(l => 
                        l.EndDate > DateTime.UtcNow && l.EndDate < DateTime.UtcNow.AddDays(7) && l.Active)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de tenants");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Crear base de datos del tenant
        /// </summary>
        private async Task CreateTenantDatabaseAsync(string tenantCode, string companyName, string adminEmail)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string no configurada");
                }

                var tenantCreator = new TenantCreator(connectionString);
                await tenantCreator.CreateTenantAsync(tenantCode, companyName, adminEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando base de datos para tenant {TenantCode}", tenantCode);
                throw;
            }
        }

        /// <summary>
        /// Eliminar base de datos del tenant
        /// </summary>
        private async Task DeleteTenantDatabaseAsync(string tenantCode)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string no configurada");
                }

                var tenantCreator = new TenantCreator(connectionString);
                await tenantCreator.DeleteTenantAsync(tenantCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando base de datos para tenant {TenantCode}", tenantCode);
                // No lanzar la excepción para no impedir la eliminación del registro
            }
        }

        /// <summary>
        /// Obtener máximo de empleados según el tipo de licencia
        /// </summary>
        private int GetMaxEmployeesForLicense(LicenseType licenseType)
        {
            return licenseType switch
            {
                LicenseType.Trial => 5,
                LicenseType.Basic => 10,
                LicenseType.Professional => 50,
                LicenseType.Enterprise => 999,
                _ => 5
            };
        }

        /// <summary>
        /// Obtener precio según el tipo de licencia
        /// </summary>
        private decimal GetPriceForLicense(LicenseType licenseType)
        {
            return licenseType switch
            {
                LicenseType.Trial => 0m,
                LicenseType.Basic => 29.99m,
                LicenseType.Professional => 79.99m,
                LicenseType.Enterprise => 199.99m,
                _ => 0m
            };
        }
    }

    /// <summary>
    /// Request para crear tenant
    /// </summary>
    public class CreateTenantRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subdomain { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public LicenseType? LicenseType { get; set; }
    }

    /// <summary>
    /// Request para actualizar tenant
    /// </summary>
    public class UpdateTenantRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subdomain { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public bool Active { get; set; } = true;
    }
}