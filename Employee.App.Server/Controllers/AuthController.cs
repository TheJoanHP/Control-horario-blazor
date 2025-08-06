using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Employee.App.Server.Data;
using Shared.Models.DTOs.Auth; // Agregado el namespace correcto
using Shared.Models.Core;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(EmployeeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Buscar usuario empleado
                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.Company)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Active);

                if (user == null || user.Employee == null)
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Credenciales inválidas" 
                    });
                }

                // Verificar contraseña
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Credenciales inválidas" 
                    });
                }

                // Generar token JWT
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.FirstName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString()),
                        new Claim("employee_id", user.Employee.Id.ToString()),
                        new Claim("company_id", user.Employee.CompanyId.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddHours(8),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _configuration["Jwt:Issuer"] ?? "EmployeeApp",
                    Audience = _configuration["Jwt:Audience"] ?? "EmployeeApp"
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = tokenString,
                    ExpiresAt = tokenDescriptor.Expires.Value,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role,
                        Active = user.Active,
                        Employee = new EmployeeInfo
                        {
                            Id = user.Employee.Id,
                            EmployeeCode = user.Employee.EmployeeCode,
                            Position = user.Employee.Position,
                            DepartmentName = user.Employee.Department?.Name,
                            Active = user.Employee.Active
                        },
                        Company = new CompanyInfo
                        {
                            Id = user.Employee.Company?.Id ?? 0,
                            Name = user.Employee.Company?.Name ?? ""
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse 
                { 
                    Success = false, 
                    Message = "Error interno del servidor" 
                });
            }
        }

        [HttpPost("pin-login")]
        public async Task<ActionResult<LoginResponse>> PinLogin([FromBody] PinLoginRequest request)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.Active);

                if (employee == null || employee.User == null)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Empleado no encontrado"
                    });
                }

                // Aquí validarías el PIN (implementar según tu lógica)
                // Por ejemplo, podrías tener un campo PIN en el empleado o usar una lógica específica
                if (request.Pin != "1234") // Placeholder - implementar lógica real
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "PIN incorrecto"
                    });
                }

                // Generar token JWT similar al login normal
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, employee.User.Id.ToString()),
                        new Claim(ClaimTypes.Name, employee.User.FirstName),
                        new Claim(ClaimTypes.Email, employee.User.Email),
                        new Claim(ClaimTypes.Role, employee.User.Role.ToString()),
                        new Claim("employee_id", employee.Id.ToString()),
                        new Claim("company_id", employee.CompanyId.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddHours(8),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _configuration["Jwt:Issuer"] ?? "EmployeeApp",
                    Audience = _configuration["Jwt:Audience"] ?? "EmployeeApp"
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login con PIN exitoso",
                    Token = tokenString,
                    ExpiresAt = tokenDescriptor.Expires.Value,
                    User = new UserInfo
                    {
                        Id = employee.User.Id,
                        Email = employee.User.Email,
                        FirstName = employee.User.FirstName,
                        LastName = employee.User.LastName,
                        Role = employee.User.Role,
                        Active = employee.User.Active,
                        Employee = new EmployeeInfo
                        {
                            Id = employee.Id,
                            EmployeeCode = employee.EmployeeCode,
                            Position = employee.Position,
                            DepartmentName = employee.Department?.Name,
                            Active = employee.Active
                        },
                        Company = new CompanyInfo
                        {
                            Id = employee.Company?.Id ?? 0,
                            Name = employee.Company?.Name ?? ""
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }
    }

    // DTOs locales para este controlador
    public class PinLoginRequest
    {
        public int EmployeeId { get; set; }
        public string Pin { get; set; } = string.Empty;
    }
}