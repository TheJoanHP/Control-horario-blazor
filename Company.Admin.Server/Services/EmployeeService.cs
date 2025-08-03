using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Services.Security;
using Shared.Models.Enums;

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

        public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
        {
            // Validar email único
            if (!await IsEmailUniqueAsync(createEmployeeDto.Email))
            {
                throw new InvalidOperationException("El email ya está en uso");
            }

            // Validar código de empleado único
            if (!await IsEmployeeCodeUniqueAsync(createEmployeeDto.EmployeeCode))
            {
                throw new InvalidOperationException("El código de empleado ya está en uso");
            }

            // Obtener la empresa (primera activa del tenant)
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Active);
            if (company == null)
            {
                throw new InvalidOperationException("No se encontró una empresa activa");
            }

            // Validar departamento si se proporciona
            if (createEmployeeDto.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == createEmployeeDto.DepartmentId && d.Active);
                if (department == null)
                {
                    throw new InvalidOperationException("Departamento no válido");
                }
            }

            var employee = new Employee
            {
                CompanyId = company.Id,
                FirstName = createEmployeeDto.FirstName.Trim(),
                LastName = createEmployeeDto.LastName.Trim(),
                Email = createEmployeeDto.Email.Trim().ToLower(),
                Phone = createEmployeeDto.Phone?.Trim(),
                EmployeeCode = createEmployeeDto.EmployeeCode.Trim().ToUpper(),
                DepartmentId = createEmployeeDto.DepartmentId,
                Role = createEmployeeDto.Role,
                PasswordHash = _passwordService.HashPassword(createEmployeeDto.Password),
                CustomWorkStartTime = createEmployeeDto.CustomWorkStartTime,
                CustomWorkEndTime = createEmployeeDto.CustomWorkEndTime,
                Active = true,
                HiredAt = createEmployeeDto.HiredAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Recargar con relaciones
            await _context.Entry(employee)
                .Reference(e => e.Company)
                .LoadAsync();
            
            await _context.Entry(employee)
                .Reference(e => e.Department)
                .LoadAsync();

            _logger.LogInformation("Empleado creado: {EmployeeId} - {EmployeeName}", employee.Id, employee.FullName);
            return employee;
        }

        public async Task<Employee> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
        {
            var employee = await _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                throw new InvalidOperationException("Empleado no encontrado");
            }

            // Validar email único (excluyendo el actual)
            if (!await IsEmailUniqueAsync(updateEmployeeDto.Email, id))
            {
                throw new InvalidOperationException("El email ya está en uso");
            }

            // Validar código único (excluyendo el actual)
            if (!await IsEmployeeCodeUniqueAsync(updateEmployeeDto.EmployeeCode, id))
            {
                throw new InvalidOperationException("El código de empleado ya está en uso");
            }

            // Validar departamento si se proporciona
            if (updateEmployeeDto.DepartmentId.HasValue)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == updateEmployeeDto.DepartmentId && d.Active);
                if (department == null)
                {
                    throw new InvalidOperationException("Departamento no válido");
                }
            }

            // Actualizar campos
            employee.FirstName = updateEmployeeDto.FirstName.Trim();
            employee.LastName = updateEmployeeDto.LastName.Trim();
            employee.Email = updateEmployeeDto.Email.Trim().ToLower();
            employee.Phone = updateEmployeeDto.Phone?.Trim();
            employee.EmployeeCode = updateEmployeeDto.EmployeeCode.Trim().ToUpper();
            employee.DepartmentId = updateEmployeeDto.DepartmentId;
            employee.Role = updateEmployeeDto.Role;
            employee.CustomWorkStartTime = updateEmployeeDto.CustomWorkStartTime;
            employee.CustomWorkEndTime = updateEmployeeDto.CustomWorkEndTime;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Empleado actualizado: {EmployeeId} - {EmployeeName}", employee.Id, employee.FullName);
            return employee;
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == email.ToLower());
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null)
        {
            var query = _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower();
                query = query.Where(e => 
                    e.FirstName.ToLower().Contains(searchTerm) ||
                    e.LastName.ToLower().Contains(searchTerm) ||
                    e.Email.ToLower().Contains(searchTerm) ||
                    e.EmployeeCode.ToLower().Contains(searchTerm));
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
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return false;
            }

            // Verificar si tiene registros de tiempo
            var hasTimeRecords = await _context.TimeRecords.AnyAsync(tr => tr.EmployeeId == id);
            if (hasTimeRecords)
            {
                throw new InvalidOperationException("No se puede eliminar un empleado que tiene registros de tiempo. Use desactivar en su lugar.");
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Empleado eliminado: {EmployeeId}", id);
            return true;
        }

        public async Task<bool> ActivateEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return false;
            }

            employee.Active = true;
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Empleado activado: {EmployeeId}", id);
            return true;
        }

        public async Task<bool> DeactivateEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return false;
            }

            employee.Active = false;
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Empleado desactivado: {EmployeeId}", id);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int id, string newPassword)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return false;
            }

            employee.PasswordHash = _passwordService.HashPassword(newPassword);
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contraseña cambiada para empleado: {EmployeeId}", id);
            return true;
        }

        public async Task<bool> ValidateEmployeeCredentialsAsync(string email, string password)
        {
            var employee = await GetEmployeeByEmailAsync(email);
            return employee != null && 
                   employee.Active && 
                   _passwordService.VerifyPassword(password, employee.PasswordHash);
        }

        public async Task<Employee?> AuthenticateEmployeeAsync(string email, string password)
        {
            var employee = await GetEmployeeByEmailAsync(email);
            
            if (employee == null || !employee.Active)
            {
                return null;
            }

            if (!_passwordService.VerifyPassword(password, employee.PasswordHash))
            {
                return null;
            }

            // Actualizar último login
            employee.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return employee;
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
            var query = _context.Employees.Where(e => e.Email == email.ToLower());
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, int? excludeId = null)
        {
            var query = _context.Employees.Where(e => e.EmployeeCode == employeeCode.ToUpper());
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId);
            }

            return !await query.AnyAsync();
        }

        public async Task<string> GenerateUniqueEmployeeCodeAsync()
        {
            var company = await _context.Companies.FirstAsync(c => c.Active);
            var companyPrefix = company.Name.Length >= 3 ? 
                company.Name.Substring(0, 3).ToUpper() : 
                company.Name.ToUpper();

            int counter = 1;
            string employeeCode;

            do
            {
                employeeCode = $"{companyPrefix}{counter:D3}";
                counter++;
            }
            while (!await IsEmployeeCodeUniqueAsync(employeeCode));

            return employeeCode;
        }
    }
}