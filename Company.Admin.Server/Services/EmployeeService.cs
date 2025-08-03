using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Company.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Models.Enums;
using Shared.Services.Security;

namespace Company.Admin.Server.Services
{
    public interface IEmployeeService
    {
        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto, int companyId);
        Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
        Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null);
        Task<Employee?> GetEmployeeByEmailAsync(string email);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);
        Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, int? excludeId = null);
        Task<bool> ActivateEmployeeAsync(int id);
        Task<bool> DeactivateEmployeeAsync(int id);
        Task<bool> ChangePasswordAsync(int id, string newPassword);
        Task<bool> UpdateLastLoginAsync(int id);
        Task<string> GenerateUniqueEmployeeCodeAsync();
        Task<int> GetTotalEmployeesAsync();
        Task<int> GetActiveEmployeesAsync();
        Task<int> GetEmployeeCountAsync(int? departmentId = null, bool? active = null);
    }

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

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto, int companyId)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(createDto.Email))
                    throw new ArgumentException("El email es requerido");

                if (string.IsNullOrWhiteSpace(createDto.EmployeeCode))
                    throw new ArgumentException("El código de empleado es requerido");

                if (!await IsEmailUniqueAsync(createDto.Email))
                    throw new InvalidOperationException($"El email {createDto.Email} ya está en uso");

                if (!await IsEmployeeCodeUniqueAsync(createDto.EmployeeCode))
                    throw new InvalidOperationException($"El código {createDto.EmployeeCode} ya está en uso");

                // Verificar que el departamento existe y pertenece a la empresa
                if (createDto.DepartmentId.HasValue)
                {
                    var department = await _context.Departments
                        .FirstOrDefaultAsync(d => d.Id == createDto.DepartmentId && d.CompanyId == companyId);
                    if (department == null)
                        throw new ArgumentException("Departamento no válido");
                }

                // Crear empleado
                var employee = _mapper.Map<Employee>(createDto);
                employee.CompanyId = companyId;
                employee.PasswordHash = _passwordService.HashPassword(createDto.Password);
                employee.Role = createDto.Role ?? UserRole.Employee;
                employee.Active = true;
                employee.HiredAt = createDto.HiredAt ?? DateTime.UtcNow;
                employee.CreatedAt = DateTime.UtcNow;
                employee.UpdatedAt = DateTime.UtcNow;

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado creado: {EmployeeCode} - {Email}", employee.EmployeeCode, employee.Email);

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando empleado con email {Email}", createDto.Email);
                throw;
            }
        }

        public async Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return null;

                // Validar email único (excluyendo el empleado actual)
                if (!string.IsNullOrWhiteSpace(updateDto.Email) && 
                    updateDto.Email != employee.Email &&
                    !await IsEmailUniqueAsync(updateDto.Email, id))
                {
                    throw new InvalidOperationException($"El email {updateDto.Email} ya está en uso");
                }

                // Validar código único (excluyendo el empleado actual)
                if (!string.IsNullOrWhiteSpace(updateDto.EmployeeCode) && 
                    updateDto.EmployeeCode != employee.EmployeeCode &&
                    !await IsEmployeeCodeUniqueAsync(updateDto.EmployeeCode, id))
                {
                    throw new InvalidOperationException($"El código {updateDto.EmployeeCode} ya está en uso");
                }

                // Verificar departamento si se proporciona
                if (updateDto.DepartmentId.HasValue)
                {
                    var department = await _context.Departments
                        .FirstOrDefaultAsync(d => d.Id == updateDto.DepartmentId && d.CompanyId == employee.CompanyId);
                    if (department == null)
                        throw new ArgumentException("Departamento no válido");
                }

                // Actualizar campos
                _mapper.Map(updateDto, employee);
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado actualizado: {EmployeeId} - {EmployeeCode}", id, employee.EmployeeCode);

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return false;

                // Verificar si el empleado tiene registros de tiempo
                var hasTimeRecords = await _context.TimeRecords.AnyAsync(tr => tr.EmployeeId == id);
                if (hasTimeRecords)
                {
                    throw new InvalidOperationException("No se puede eliminar un empleado que tiene registros de tiempo. Use la desactivación en su lugar.");
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado eliminado: {EmployeeId} - {EmployeeCode}", id, employee.EmployeeCode);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.Id == id);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado {EmployeeId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Company)
                    .AsQueryable();

                // Filtro por búsqueda
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(e => 
                        e.FirstName.ToLower().Contains(search) ||
                        e.LastName.ToLower().Contains(search) ||
                        e.Email.ToLower().Contains(search) ||
                        e.EmployeeCode.ToLower().Contains(search));
                }

                // Filtro por departamento
                if (departmentId.HasValue)
                {
                    query = query.Where(e => e.DepartmentId == departmentId);
                }

                // Filtro por estado activo
                if (active.HasValue)
                {
                    query = query.Where(e => e.Active == active);
                }

                var employees = await query
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados");
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            try
            {
                return await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado por email {Email}", email);
                throw;
            }
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
        {
            try
            {
                var query = _context.Employees.Where(e => e.Email == email);

                if (excludeId.HasValue)
                    query = query.Where(e => e.Id != excludeId);

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando unicidad de email {Email}", email);
                throw;
            }
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, int? excludeId = null)
        {
            try
            {
                var query = _context.Employees.Where(e => e.EmployeeCode == employeeCode);

                if (excludeId.HasValue)
                    query = query.Where(e => e.Id != excludeId);

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando unicidad de código {Code}", employeeCode);
                throw;
            }
        }

        public async Task<bool> ActivateEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return false;

                employee.Active = true;
                employee.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado activado: {EmployeeId} - {EmployeeCode}", id, employee.EmployeeCode);

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
                if (employee == null)
                    return false;

                employee.Active = false;
                employee.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado desactivado: {EmployeeId} - {EmployeeCode}", id, employee.EmployeeCode);

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
                if (employee == null)
                    return false;

                employee.PasswordHash = _passwordService.HashPassword(newPassword);
                employee.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contraseña cambiada para empleado: {EmployeeId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña del empleado {Id}", id);
                throw;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return false;

                employee.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando último login del empleado {Id}", id);
                throw;
            }
        }

        public async Task<string> GenerateUniqueEmployeeCodeAsync()
        {
            try
            {
                string code;
                bool isUnique;
                int attempts = 0;
                const int maxAttempts = 100;

                do
                {
                    attempts++;
                    if (attempts > maxAttempts)
                        throw new InvalidOperationException("No se pudo generar un código único después de múltiples intentos");

                    // Generar código en formato EMP + 4 dígitos
                    var number = Random.Shared.Next(1000, 9999);
                    code = $"EMP{number}";
                    
                    isUnique = await IsEmployeeCodeUniqueAsync(code);
                } 
                while (!isUnique);

                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando código único de empleado");
                throw;
            }
        }

        public async Task<int> GetTotalEmployeesAsync()
        {
            try
            {
                return await _context.Employees.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo total de empleados");
                throw;
            }
        }

        public async Task<int> GetActiveEmployeesAsync()
        {
            try
            {
                return await _context.Employees.CountAsync(e => e.Active);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados activos");
                throw;
            }
        }

        public async Task<int> GetEmployeeCountAsync(int? departmentId = null, bool? active = null)
        {
            try
            {
                var query = _context.Employees.AsQueryable();

                if (departmentId.HasValue)
                    query = query.Where(e => e.DepartmentId == departmentId);

                if (active.HasValue)
                    query = query.Where(e => e.Active == active);

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo conteo de empleados con filtros");
                throw;
            }
        }
    }
}