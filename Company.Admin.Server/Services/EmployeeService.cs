using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Models.Enums;
using Shared.Services.Security;

namespace Company.Admin.Server.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly CompanyDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            CompanyDbContext context,
            IPasswordService passwordService,
            ILogger<EmployeeService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto)
        {
            try
            {
                // Verificar unicidad del email
                if (!await IsEmailUniqueAsync(createDto.Email))
                {
                    throw new InvalidOperationException("El email ya está en uso");
                }

                // Generar código único si no se proporciona
                var employeeCode = !string.IsNullOrEmpty(createDto.EmployeeCode) 
                    ? createDto.EmployeeCode 
                    : await GenerateUniqueEmployeeCodeAsync();

                // Verificar unicidad del código
                if (!await IsEmployeeCodeUniqueAsync(employeeCode))
                {
                    throw new InvalidOperationException("El código de empleado ya está en uso");
                }

                // Crear usuario
                var user = new User
                {
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    Email = createDto.Email,
                    PasswordHash = _passwordService.HashPassword(createDto.Password ?? "123456"),
                    Role = UserRole.Employee,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Crear empleado
                var employee = new Employee
                {
                    UserId = user.Id,
                    CompanyId = createDto.CompanyId,
                    DepartmentId = createDto.DepartmentId,
                    EmployeeCode = employeeCode,
                    Position = createDto.Position,
                    Phone = createDto.Phone,
                    HireDate = createDto.HireDate ?? DateTime.Today,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Cargar relaciones para el DTO
                await _context.Entry(employee)
                    .Reference(e => e.User)
                    .LoadAsync();

                await _context.Entry(employee)
                    .Reference(e => e.Department)
                    .LoadAsync();

                return MapToDto(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando empleado");
                throw;
            }
        }

        public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    throw new ArgumentException("Empleado no encontrado");
                }

                // Actualizar datos del usuario si se proporcionan
                if (!string.IsNullOrEmpty(updateDto.FirstName))
                    employee.User.FirstName = updateDto.FirstName;

                if (!string.IsNullOrEmpty(updateDto.LastName))
                    employee.User.LastName = updateDto.LastName;

                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != employee.User.Email)
                {
                    if (!await IsEmailUniqueAsync(updateDto.Email, id))
                    {
                        throw new InvalidOperationException("El email ya está en uso");
                    }
                    employee.User.Email = updateDto.Email;
                }

                // Actualizar datos del empleado
                if (!string.IsNullOrEmpty(updateDto.EmployeeCode) && updateDto.EmployeeCode != employee.EmployeeCode)
                {
                    if (!await IsEmployeeCodeUniqueAsync(updateDto.EmployeeCode, id))
                    {
                        throw new InvalidOperationException("El código de empleado ya está en uso");
                    }
                    employee.EmployeeCode = updateDto.EmployeeCode;
                }

                if (updateDto.DepartmentId.HasValue)
                    employee.DepartmentId = updateDto.DepartmentId;

                if (!string.IsNullOrEmpty(updateDto.Position))
                    employee.Position = updateDto.Position;

                if (!string.IsNullOrEmpty(updateDto.Phone))
                    employee.Phone = updateDto.Phone;

                if (updateDto.HireDate.HasValue)
                    employee.HireDate = updateDto.HireDate.Value;

                if (updateDto.Active.HasValue)
                {
                    employee.Active = updateDto.Active.Value;
                    employee.User.Active = updateDto.Active.Value;
                }

                employee.UpdatedAt = DateTime.UtcNow;
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return MapToDto(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == id);

            return employee != null ? MapToDto(employee) : null;
        }

        public async Task<EmployeeDto?> GetEmployeeByEmailAsync(string email)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.User.Email == email);

            return employee != null ? MapToDto(employee) : null;
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null)
        {
            var query = _context.Employees
                .Include(e => e.User)
                .Include(e => e.Department)
                .Include(e => e.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(e => 
                    e.User.FirstName.ToLower().Contains(search) ||
                    e.User.LastName.ToLower().Contains(search) ||
                    e.User.Email.ToLower().Contains(search) ||
                    e.EmployeeCode.ToLower().Contains(search));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            if (active.HasValue)
            {
                query = query.Where(e => e.Active == active);
            }

            var employees = await query
                .OrderBy(e => e.User.LastName)
                .ThenBy(e => e.User.FirstName)
                .ToListAsync();

            return employees.Select(MapToDto);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null) return false;

                // Soft delete - marcar como inactivo
                employee.Active = false;
                employee.User.Active = false;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando empleado {EmployeeId}", id);
                return false;
            }
        }

        public async Task<bool> ActivateEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null) return false;

                employee.Active = true;
                employee.User.Active = true;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando empleado {EmployeeId}", id);
                return false;
            }
        }

        public async Task<bool> DeactivateEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null) return false;

                employee.Active = false;
                employee.User.Active = false;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando empleado {EmployeeId}", id);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int id, string newPassword)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null) return false;

                employee.User.PasswordHash = _passwordService.HashPassword(newPassword);
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña del empleado {EmployeeId}", id);
                return false;
            }
        }

        public async Task<bool> ValidateEmployeeCredentialsAsync(string email, string password)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.User.Email == email && e.Active);

            if (employee == null) return false;

            return _passwordService.VerifyPassword(password, employee.User.PasswordHash);
        }

        public async Task<EmployeeDto?> AuthenticateEmployeeAsync(string email, string password)
        {
            if (await ValidateEmployeeCredentialsAsync(email, password))
            {
                return await GetEmployeeByEmailAsync(email);
            }
            return null;
        }

        public async Task<int> GetTotalEmployeesAsync()
        {
            return await _context.Employees.CountAsync();
        }

        public async Task<int> GetActiveEmployeesAsync()
        {
            return await _context.Employees.CountAsync(e => e.Active);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeEmployeeId = null)
        {
            var query = _context.Users.Where(u => u.Email == email);
            
            if (excludeEmployeeId.HasValue)
            {
                query = query.Where(u => u.Employee == null || u.Employee.Id != excludeEmployeeId);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string code, int? excludeEmployeeId = null)
        {
            var query = _context.Employees.Where(e => e.EmployeeCode == code);
            
            if (excludeEmployeeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeEmployeeId);
            }

            return !await query.AnyAsync();
        }

        public async Task<string> GenerateUniqueEmployeeCodeAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"EMP{year}";
            
            var lastEmployee = await _context.Employees
                .Where(e => e.EmployeeCode.StartsWith(prefix))
                .OrderByDescending(e => e.EmployeeCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEmployee != null)
            {
                var lastNumberStr = lastEmployee.EmployeeCode.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        // Método auxiliar para mapear Employee a EmployeeDto
        private EmployeeDto MapToDto(Employee employee)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                CompanyId = employee.CompanyId,
                DepartmentId = employee.DepartmentId,
                EmployeeCode = employee.EmployeeCode,
                FirstName = employee.User?.FirstName ?? "",
                LastName = employee.User?.LastName ?? "",
                Email = employee.User?.Email ?? "",
                Position = employee.Position,
                Phone = employee.Phone,
                HireDate = employee.HireDate,
                Active = employee.Active,
                DepartmentName = employee.Department?.Name,
                CompanyName = employee.Company?.Name,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt
            };
        }
    }
}