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

                // Cargar relaciones para el DTO
                await _context.Entry(employee)
                    .Reference(e => e.Company)
                    .LoadAsync();

                if (employee.DepartmentId.HasValue)
                {
                    await _context.Entry(employee)
                        .Reference(e => e.Department)
                        .LoadAsync();
                }

                return _mapper.Map<EmployeeDto>(employee);
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
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    throw new KeyNotFoundException($"Empleado con ID {id} no encontrado");

                // Validaciones
                if (!string.IsNullOrEmpty(updateDto.Email) && 
                    !await IsEmailUniqueAsync(updateDto.Email, id))
                    throw new InvalidOperationException($"El email {updateDto.Email} ya está en uso");

                if (!string.IsNullOrEmpty(updateDto.EmployeeCode) && 
                    !await IsEmployeeCodeUniqueAsync(updateDto.EmployeeCode, id))
                    throw new InvalidOperationException($"El código {updateDto.EmployeeCode} ya está en uso");

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

                // Actualizar contraseña si se proporciona
                if (!string.IsNullOrEmpty(updateDto.Password))
                {
                    employee.PasswordHash = _passwordService.HashPassword(updateDto.Password);
                }

                await _context.SaveChangesAsync();

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {Id}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado {Id}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByEmailAsync(string email)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == email);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado por email {Email}", email);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string employeeCode)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado por código {Code}", employeeCode);
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(int? departmentId = null, bool? active = null)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .AsQueryable();

                if (departmentId.HasValue)
                    query = query.Where(e => e.DepartmentId == departmentId);

                if (active.HasValue)
                    query = query.Where(e => e.Active == active);

                var employees = await query
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados");
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> GetActiveEmployeesAsync()
        {
            return await GetEmployeesAsync(active: true);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return false;

                // Verificar si el empleado tiene registros de tiempo
                var hasTimeRecords = await _context.TimeRecords
                    .AnyAsync(tr => tr.EmployeeId == id);

                if (hasTimeRecords)
                {
                    // Solo desactivar si tiene registros
                    employee.Active = false;
                    employee.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Eliminar completamente si no tiene registros
                    _context.Employees.Remove(employee);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando empleado {Id}", id);
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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando empleado {Id}", id);
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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando empleado {Id}", id);
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
                _logger.LogError(ex, "Error obteniendo conteo de empleados");
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> SearchEmployeesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetActiveEmployeesAsync();

                var employees = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .Where(e => e.Active &&
                               (e.FirstName.Contains(searchTerm) ||
                                e.LastName.Contains(searchTerm) ||
                                e.Email.Contains(searchTerm) ||
                                e.EmployeeCode.Contains(searchTerm)))
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando empleados con término {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email && e.Active);

                if (employee == null)
                    return false;

                return _passwordService.VerifyPassword(password, employee.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando credenciales para {Email}", email);
                throw;
            }
        }

        public async Task<EmployeeDto?> AuthenticateAsync(string email, string password)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == email && e.Active);

                if (employee == null)
                    return null;

                var isValidPassword = _passwordService.VerifyPassword(password, employee.PasswordHash);
                if (!isValidPassword)
                    return null;

                // Actualizar último login
                employee.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error autenticando empleado {Email}", email);
                throw;
            }
        }
    }e => e.Id == id);

                if (employee == null)
                    throw new KeyNotFoundException($"Empleado con ID {id} no encontrado");

                // Validaciones
                if (!string.IsNullOrEmpty(updateDto.Email) && 
                    !await IsEmailUniqueAsync(updateDto.Email, id))
                    throw new InvalidOperationException($"El email {updateDto.Email} ya está en uso");

                if (!string.IsNullOrEmpty(updateDto.EmployeeCode) && 
                    !await IsEmployeeCodeUniqueAsync(updateDto.EmployeeCode, id))
                    throw new InvalidOperationException($"El código {updateDto.EmployeeCode} ya está en uso");

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

                // Actualizar contraseña si se proporciona
                if (!string.IsNullOrEmpty(updateDto.Password))
                {
                    employee.PasswordHash = await _passwordService.HashPasswordAsync(updateDto.Password);
                }

                await _context.SaveChangesAsync();

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {Id}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado {Id}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByEmailAsync(string email)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == email);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado por email {Email}", email);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string employeeCode)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado por código {Code}", employeeCode);
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(int? departmentId = null, bool? active = null)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .AsQueryable();

                if (departmentId.HasValue)
                    query = query.Where(e => e.DepartmentId == departmentId);

                if (active.HasValue)
                    query = query.Where(e => e.Active == active);

                var employees = await query
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados");
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> GetActiveEmployeesAsync()
        {
            return await GetEmployeesAsync(active: true);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return false;

                // Verificar si el empleado tiene registros de tiempo
                var hasTimeRecords = await _context.TimeRecords
                    .AnyAsync(tr => tr.EmployeeId == id);

                if (hasTimeRecords)
                {
                    // Solo desactivar si tiene registros
                    employee.Active = false;
                    employee.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Eliminar completamente si no tiene registros
                    _context.Employees.Remove(employee);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando empleado {Id}", id);
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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando empleado {Id}", id);
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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando empleado {Id}", id);
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

        public async Task<bool> ChangePasswordAsync(int id, string newPassword)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                    return false;

                employee.PasswordHash = await _passwordService.HashPasswordAsync(newPassword);
                employee.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

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
                _logger.LogError(ex, "Error obteniendo conteo de empleados");
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeDto>> SearchEmployeesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetActiveEmployeesAsync();

                var employees = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .Where(e => e.Active &&
                               (e.FirstName.Contains(searchTerm) ||
                                e.LastName.Contains(searchTerm) ||
                                e.Email.Contains(searchTerm) ||
                                e.EmployeeCode.Contains(searchTerm)))
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando empleados con término {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email && e.Active);

                if (employee == null)
                    return false;

                return await _passwordService.VerifyPasswordAsync(password, employee.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando credenciales para {Email}", email);
                throw;
            }
        }

        public async Task<EmployeeDto?> AuthenticateAsync(string email, string password)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == email && e.Active);

                if (employee == null)
                    return null;

                var isValidPassword = await _passwordService.VerifyPasswordAsync(password, employee.PasswordHash);
                if (!isValidPassword)
                    return null;

                // Actualizar último login
                employee.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error autenticando empleado {Email}", email);
                throw;
            }
        }
    }