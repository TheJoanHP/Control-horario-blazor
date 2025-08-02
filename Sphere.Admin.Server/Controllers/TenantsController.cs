using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sphere.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.Enums;

namespace Sphere.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TenantsController : ControllerBase
    {
        private readonly SphereDbContext _context;

        public TenantsController(SphereDbContext context)
        {
            _context = context;
        }

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
                return BadRequest(new { message = "Error obteniendo tenants" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetTenant(int id)
        {
            try
            {
                var tenant = await _context.Tenants
                    .Include(t => t.License)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                    return NotFound();

                return Ok(tenant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo tenant" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            try
            {
                // Verificar que el código no exista
                if (await _context.Tenants.AnyAsync(t => t.Code == request.Code))
                {
                    return BadRequest(new { message = "El código del tenant ya existe" });
                }

                var tenant = new Tenant
                {
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    ContactEmail = request.ContactEmail,
                    ContactPhone = request.ContactPhone,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                // Crear licencia básica
                var license = new License
                {
                    TenantId = tenant.Id,
                    Type = LicenseType.Trial,
                    MaxEmployees = 10,
                    HasReports = false,
                    HasAPI = false,
                    HasMobileApp = true,
                    MonthlyPrice = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Licenses.Add(license);
                await _context.SaveChangesAsync();

                // Cargar el tenant con la licencia
                tenant = await _context.Tenants
                    .Include(t => t.License)
                    .FirstAsync(t => t.Id == tenant.Id);

                return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error creando tenant" });
            }
        }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
    }
}