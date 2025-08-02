using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Company.Admin.Server.Data;
using Shared.Models.DTOs;
using Shared.Models.Core;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CompanyDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(CompanyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Buscar usuario (admin de empresa o supervisor)
                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.Company)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Active);

                if (user == null)
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

                // Verificar que sea admin o supervisor
                if (user.Role != "COMPANY_ADMIN" && user.Role != "SUPERVISOR")
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "No tienes permisos para acceder al panel de administración" 
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
                        Employee = user.Employee != null ? new EmployeeDto
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
                        } : null
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

        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.Company)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return NotFound();

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    Active = user.Active,
                    LastLogin = user.LastLogin,
                    Employee = user.Employee != null ? new EmployeeDto
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
                    } : null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo perfil" });
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
}