using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeApp.Server.Data;
using Shared.Models.DTOs;
using Shared.Models.Core;

namespace EmployeeApp.Server.Controllers
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

                // Actualizar último login
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generar JWT
                var token = GenerateJwtToken(user);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Login exitoso",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Role = user.Role,
                        Active = user.Active,
                        LastLogin = user.LastLogin,
                        Employee = new EmployeeDto
                        {
                            Id = user.Employee.Id,
                            EmployeeCode = user.Employee.EmployeeCode,
                            Department = user.Employee.Department,
                            Position = user.Employee.Position,
                            HireDate = user.Employee.HireDate,
                            Company = new CompanyDto
                            {
                                Id = user.Employee.Company.Id,
                                Name = user.Employee.Company.Name
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new LoginResponse 
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
                // Buscar empleado por código y PIN
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.EmployeeCode == request.EmployeeCode && 
                                            e.Active && e.User.Active);

                if (employee == null || string.IsNullOrEmpty(employee.Pin))
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Código de empleado o PIN inválido" 
                    });
                }

                // Verificar PIN
                if (!BCrypt.Net.BCrypt.Verify(request.Pin, employee.Pin))
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Código de empleado o PIN inválido" 
                    });
                }

                // Actualizar último login
                employee.User.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generar JWT
                var token = GenerateJwtToken(employee.User);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Login exitoso",
                    User = new UserDto
                    {
                        Id = employee.User.Id,
                        Name = employee.User.Name,
                        Email = employee.User.Email,
                        Role = employee.User.Role,
                        Active = employee.User.Active,
                        LastLogin = employee.User.LastLogin,
                        Employee = new EmployeeDto
                        {
                            Id = employee.Id,
                            EmployeeCode = employee.EmployeeCode,
                            Department = employee.Department,
                            Position = employee.Position,
                            HireDate = employee.HireDate,
                            Company = new CompanyDto
                            {
                                Id = employee.Company.Id,
                                Name = employee.Company.Name
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Error interno del servidor" 
                });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class PinLoginRequest
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }
}