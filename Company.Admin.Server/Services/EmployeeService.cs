using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Models.Enums;
using Shared.Services.Security;
using Company.Admin.Server.Data;

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

        public async Task<IEnumerable<EmployeeDto>> GetAllAsync(int? departmentId = null, bool? active = null)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .Include(e => e.WorkSchedule)
                    .AsQueryable();

                if (departmentId.HasValue)
                    query = query.Where(e => e.DepartmentId == departmentId.Value);

                if (active.HasValue)
                    query = query.Where(e => e.Active == active.Value);

                var employees = await query
                    .OrderBy(e => e.User.LastName)
                    .ThenBy(e => e.User.FirstName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados");
                throw;
            }
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .Include(e => e.WorkSchedule)
                    .FirstOrDefaultAsync(e => e.Id == id);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleado {Id}", id);
                throw;
            }
        }

        public async Task<EmployeeDto?> GetByEmployeeNumberAsync(string employeeNumber)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .Include(e => e.WorkSchedule)
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber);

                return employee != null ? _mapper.Map<EmployeeDto>(employee) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleado por número {EmployeeNumber}", employeeNumber);
                throw;
            }
        }

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto createDto)
        {
            try
            {
                // Verificar que el email no exista
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == createDto.Email);

                if (existingUser != null)
                    throw new InvalidOperationException("Ya existe un usuario con este email");

                // Verificar que el número de empleado no exista
                if (!string.IsNullOrEmpty(createDto.EmployeeNumber))
                {
                    var existingEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeNumber == createDto.EmployeeNumber);

                    if (existingEmployee != null)
                        throw new InvalidOperationException("Ya existe un empleado con este número");
                }

                // Generar número de empleado si no se proporciona
                var employeeNumber = createDto.EmployeeNumber;
                if (string.IsNullOrEmpty(employeeNumber))
                {
                    employeeNumber = await GenerateEmployeeNumberAsync();
                }

                // Crear usuario
                var user = new User
                {
                    CompanyId = GetCurrentCompanyId(),
                    Username = createDto.Username ?? createDto.Email.Split('@')[0],
                    Email = createDto.Email,
                    PasswordHash = _passwordService.HashPassword(createDto.Password ?? "empleado123"),
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    Role = createDto.Role ?? UserRole.Employee,
                    Active = createDto.Active ?? true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Crear empleado
                var employee = new Employee
                {
                    UserId = user.Id,
                    CompanyId = GetCurrentCompanyId(),
                    DepartmentId = createDto.DepartmentId,
                    EmployeeNumber = employeeNumber,
                    Position = createDto.Position,
                    HireDate = createDto.HireDate ?? DateTime.Today,
                    WorkScheduleId = createDto.WorkScheduleId,
                    Salary = createDto.Salary,
                    Active = createDto.Active ?? true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Recargar con relaciones
                employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .Include(e => e.WorkSchedule)
                    .FirstAsync(e => e.Id == employee.Id);

                _logger.LogInformation("Empleado creado: {EmployeeNumber} - {Name}", 
                    employee.EmployeeNumber, employee.User.FullName);

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado");
                throw;
            }
        }

        public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto updateDto)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    throw new ArgumentException("Empleado no encontrado");

                // Actualizar usuario
                if (!string.IsNullOrEmpty(updateDto.FirstName))
                    employee.User.FirstName = updateDto.FirstName;

                if (!string.IsNullOrEmpty(updateDto.LastName))
                    employee.User.LastName = updateDto.LastName;

                if (!string.IsNullOrEmpty(updateDto.Email))
                {
                    // Verificar que el email no exista en otro usuario
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == updateDto.Email && u.Id != employee.UserId);

                    if (existingUser != null)
                        throw new InvalidOperationException("Ya existe un usuario con este email");

                    employee.User.Email = updateDto.Email;
                }

                if (updateDto.Role.HasValue)
                    employee.User.Role = updateDto.Role.Value;

                if (updateDto.Active.HasValue)
                {
                    employee.User.Active = updateDto.Active.Value;
                    employee.Active = updateDto.Active.Value;
                }

                // Actualizar empleado
                if (!string.IsNullOrEmpty(updateDto.EmployeeNumber))
                {
                    // Verificar que el número no exista en otro empleado
                    var existingEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeNumber == updateDto.EmployeeNumber && e.Id != id);

                    if (existingEmployee != null)
                        throw new InvalidOperationException("Ya existe un empleado con este número");

                    employee.EmployeeNumber = updateDto.EmployeeNumber;
                }

                if (!string.IsNullOrEmpty(updateDto.Position))
                    employee.Position = updateDto.Position;

                if (updateDto.DepartmentId.HasValue)
                    employee.DepartmentId = updateDto.DepartmentId;

                if (updateDto.WorkScheduleId.HasValue)
                    employee.WorkScheduleId = updateDto.WorkScheduleId;

                if (updateDto.HireDate.HasValue)
                    employee.HireDate = updateDto.HireDate.Value;

                if (updateDto.Salary.HasValue)
                    employee.Salary = updateDto.Salary;

                employee.UpdatedAt = DateTime.UtcNow;
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Recargar con relaciones
                employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .Include(e => e.WorkSchedule)
                    .FirstAsync(e => e.Id == id);

                _logger.LogInformation("Empleado actualizado: {EmployeeNumber} - {Name}", 
                    employee.EmployeeNumber, employee.User.FullName);

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                    return false;

                // Soft delete - marcar como inactivo
                employee.Active = false;
                employee.User.Active = false;
                employee.UpdatedAt = DateTime.UtcNow;
                employee.User.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado desactivado: {EmployeeNumber} - {Name}", 
                    employee.EmployeeNumber, employee.User.FullName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> ExistsByEmployeeNumberAsync(string employeeNumber)
        {
            return await _context.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber);
        }

        public async Task<IEnumerable<EmployeeDto>> GetByDepartmentAsync(int departmentId)
        {
            try
            {
                var employees = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Department)
                    .Include(e => e.WorkSchedule)
                    .Where(e => e.DepartmentId == departmentId)
                    .OrderBy(e => e.User.LastName)
                    .ThenBy(e => e.User.FirstName)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados del departamento {DepartmentId}", departmentId);
                throw;
            }
        }

        private async Task<string> GenerateEmployeeNumberAsync()
        {
            var lastEmployee = await _context.Employees
                .Where(e => e.EmployeeNumber.StartsWith("EMP"))
                .OrderByDescending(e => e.EmployeeNumber)
                .FirstOrDefaultAsync();

            var lastNumber = 0;
            if (lastEmployee != null && lastEmployee.EmployeeNumber.Length > 3)
            {
                var numberPart = lastEmployee.EmployeeNumber[3..];
                int.TryParse(numberPart, out lastNumber);
            }

            return $"EMP{(lastNumber + 1):D3}";
        }

        private int GetCurrentCompanyId()
        {
            // En una implementación real, esto vendría del contexto del tenant
            // Por ahora, asumimos que hay una empresa por defecto
            return 1;
        }
    }
}