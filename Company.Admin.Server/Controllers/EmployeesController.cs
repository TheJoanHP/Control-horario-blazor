using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Core;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly CompanyDbContext _context;

        public EmployeesController(CompanyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            try
            {
                var employees = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Company)
                    .Where(e => e.Active)
                    .OrderBy(e => e.User.Name)
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo empleados" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return NotFound();

                return Ok(employee);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo empleado" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                // Verificar que el email no exista
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "El email ya está en uso" });
                }

                // Verificar que la empresa exista
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId);
                if (company == null)
                {
                    return BadRequest(new { message = "La empresa no existe" });
                }

                // Crear usuario
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = request.Role,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Crear empleado
                var employee = new Employee
                {
                    UserId = user.Id,
                    CompanyId = request.CompanyId,
                    EmployeeCode = request.EmployeeCode,
                    Department = request.Department,
                    Position = request.Position,
                    HireDate = request.HireDate,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Cargar el empleado completo
                employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Company)
                    .FirstAsync(e => e.Id == employee.Id);

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error creando empleado" });
            }
        }
    }

    public class CreateEmployeeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "EMPLOYEE";
        public int CompanyId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
    }
}   