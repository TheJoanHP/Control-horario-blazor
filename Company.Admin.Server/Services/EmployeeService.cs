using Microsoft.EntityFrameworkCore;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            CompanyDbContext context,
            IMapper mapper,
            IPasswordService passwordService,
            ILogger<EmployeeService> logger)
        {
            _context = context;
            _mapper = mapper;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
        {
            try
            {
                // Validar que el email no exista
                var existingEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == createEmployeeDto.Email);
                
                if (existingEmployee != null)
                {
                    throw new InvalidOperationException("Ya existe un empleado con este email");
                }

                // Generar código único de empleado si no se proporciona
                var employeeCode = string.IsNullOrEmpty(createEmployeeDto.EmployeeCode) 
                    ? await GenerateUniqueEmployeeCodeAsync()
                    : createEmployeeDto.EmployeeCode;

                // Validar que el código de empleado no exista
                var existingCode = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);
                
                if (existingCode != null)
                {
                    throw new InvalidOperationException("Ya existe un empleado con este código");
                }

                var employee = new Employee
                {
                    FirstName = createEmployeeDto.FirstName,
                    LastName = createEmployeeDto.LastName,
                    Email = createEmployeeDto.Email,
                    EmployeeCode = employeeCode,
                    Phone = createEmployeeDto.Phone,
                    DepartmentId = createEmployeeDto.DepartmentId,
                    Position = createEmployeeDto.Position,
                    Role = createEmployeeDto.Role,
                    HireDate = createEmployeeDto.HireDate,
                    Salary = createEmployeeDto.Salary,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = _passwordService.HashPassword(createEmployeeDto.Password ?? "123456") // Password temporal
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado creado: {EmployeeId} - {Email}", employee.Id, employee.Email);
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando empleado");
                throw;
            }
        }

        public async Task<Employee> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    throw new InvalidOperationException("Empleado no encontrado");
                }

                // Validar email único (excluyendo el empleado actual)
                if (!string.IsNullOrEmpty(updateEmployeeDto.Email) && updateEmployeeDto.Email != employee.Email)
                {
                    var existingEmail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Email == updateEmployeeDto.Email && e.Id != id);
                    
                    if (existingEmail != null)
                    {
                        throw new InvalidOperationException("Ya existe un empleado con este email");
                    }
                    employee.Email = updateEmployeeDto.Email;
                }

                // Actualizar campos
                employee.FirstName = updateEmployeeDto.FirstName ?? employee.FirstName;
                employee.LastName = updateEmployeeDto.LastName ?? employee.LastName;
                employee.Phone = updateEmployeeDto.Phone ?? employee.Phone;
                employee.DepartmentId = updateEmployeeDto.DepartmentId ?? employee.DepartmentId;
                employee.Position = updateEmployeeDto.Position ?? employee.Position;
                employee.Role = updateEmployeeDto.Role ?? employee.Role;
                employee.Salary = updateEmployeeDto.Salary ?? employee.Salary;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado actualizado: {EmployeeId}", employee.Id);
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == email);
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => 
                    e.FirstName.Contains(search) || 
                    e.LastName.Contains(search) || 
                    e.Email.Contains(search) ||
                    e.EmployeeCode.Contains(search));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            if (active.HasValue)
            {
                query = query.Where(e => e.Active == active);
            }

            return await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return false;

                // Soft delete
                employee.Active = false;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado eliminado (soft): {EmployeeId}", employee.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return false;

                employee.Active = true;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return false;

                employee.Active = false;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int id, string newPassword)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return false;

                employee.PasswordHash = _passwordService.HashPassword(newPassword);
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña de empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<bool> ValidateEmployeeCredentialsAsync(string email, string password)
        {
            try
            {
                var employee = await GetEmployeeByEmailAsync(email);
                if (employee == null || !employee.Active)
                    return false;

                return _passwordService.VerifyPassword(password, employee.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando credenciales para {Email}", email);
                return false;
            }
        }

        public async Task<Employee?> AuthenticateEmployeeAsync(string email, string password)
        {
            try
            {
                var employee = await GetEmployeeByEmailAsync(email);
                if (employee == null || !employee.Active)
                    return null;

                var isValid = _passwordService.VerifyPassword(password, employee.PasswordHash);
                if (!isValid)
                    return null;

                // Actualizar último login
                await UpdateLastLoginAsync(employee.Id);

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error autenticando empleado {Email}", email);
                return null;
            }
        }

        public async Task<int> GetTotalEmployeesAsync()
        {
            return await _context.Employees.CountAsync();
        }

        public async Task<int> GetActiveEmployeesAsync()
        {
            return await _context.Employees.CountAsync(e => e.Active);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
        {
            var query = _context.Employees.Where(e => e.Email == email);
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, int? excludeId = null)
        {
            var query = _context.Employees.Where(e => e.EmployeeCode == employeeCode);
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId);
            }

            return !await query.AnyAsync();
        }

        public async Task<string> GenerateUniqueEmployeeCodeAsync()
        {
            string code;
            bool isUnique;
            
            do 
            {
                code = $"EMP{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
                isUnique = await IsEmployeeCodeUniqueAsync(code);
            } 
            while (!isUnique);

            return code;
        }

        private async Task<bool> UpdateLastLoginAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return false;

                employee.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando último login de empleado {EmployeeId}", id);
                return false;
            }
        }
    }
}