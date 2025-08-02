using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Services.Security;
using Shared.Services.Communication;

namespace Company.Admin.Server.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly CompanyDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            CompanyDbContext context,
            IPasswordService passwordService,
            IEmailService emailService,
            ILogger<EmployeeService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
        {
            // Validar email único
            if (!await IsEmailUniqueAsync(createEmployeeDto.Email))
            {
                throw new InvalidOperationException("Ya existe un empleado con este email");
            }

            // Validar código de empleado único si se proporciona
            if (!string.IsNullOrEmpty(createEmployeeDto.EmployeeCode))
            {
                if (!await IsEmployeeCodeUniqueAsync(createEmployeeDto.EmployeeCode))
                {
                    throw new InvalidOperationException("Ya existe un empleado con este código");
                }
            }
            else
            {
                // Generar código único automáticamente
                createEmployeeDto.EmployeeCode = await GenerateUniqueEmployeeCodeAsync();
            }

            // Obtener la empresa (primera que esté activa, ya que estamos en el contexto del tenant)
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Active);
            if (company == null)
            {
                throw new InvalidOperationException("No se encontró una empresa activa para este tenant");
            }

            // Crear el empleado
            var employee = new Employee
            {
                CompanyId = company.Id,
                DepartmentId = createEmployeeDto.DepartmentId,
                FirstName = createEmployeeDto.FirstName.Trim(),
                LastName = createEmployeeDto.LastName.Trim(),
                Email = createEmployeeDto.Email.Trim().ToLowerInvariant(),
                Phone = createEmployeeDto.Phone?.Trim(),
                EmployeeCode = createEmployeeDto.EmployeeCode,
                Role = createEmployeeDto.Role,
                PasswordHash = _passwordService.HashPassword(createEmployeeDto.Password),
                Active = createEmployeeDto.Active,
                HiredAt = createEmployeeDto.HiredAt ?? DateTime.UtcNow,
                CustomWorkStartTime = createEmployeeDto.CustomWorkStartTime,
                CustomWorkEndTime = createEmployeeDto.CustomWorkEndTime,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Enviar email de bienvenida (opcional, en background)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(
                        employee.Email,
                        employee.FullName,
                        createEmployeeDto.Password,
                        company.Name
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo enviar email de bienvenida a {Email}", employee.Email);
                }
            });

            _logger.LogInformation("Empleado creado: {EmployeeId} - {EmployeeName}", employee.Id, employee.FullName);
            return employee;
        }

        public async Task<Employee> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                throw new ArgumentException($"Empleado con ID {id} no encontrado");
            }

            // Validar email único si se está cambiando
            if (!string.IsNullOrEmpty(updateEmployeeDto.Email) && 
                updateEmployeeDto.Email != employee.Email)
            {
                if (!await IsEmailUniqueAsync(updateEmployeeDto.Email, id))
                {
                    throw new InvalidOperationException("Ya existe un empleado con este email");
                }
                employee.Email = updateEmployeeDto.Email.Trim().ToLowerInvariant();
            }

            // Validar código de empleado único si se está cambiando
            if (!string.IsNullOrEmpty(updateEmployeeDto.EmployeeCode) && 
                updateEmployeeDto.EmployeeCode != employee.EmployeeCode)
            {
                if (!await IsEmployeeCodeUniqueAsync(updateEmployeeDto.EmployeeCode, id))
                {
                    throw new InvalidOperationException("Ya existe un empleado con este código");
                }
                employee.EmployeeCode = updateEmployeeDto.EmployeeCode;
            }

            // Actualizar campos
            employee.FirstName = updateEmployeeDto.FirstName.Trim();
            employee.LastName = updateEmployeeDto.LastName.Trim();
            
            if (!string.IsNullOrEmpty(updateEmployeeDto.Phone))
                employee.Phone = updateEmployeeDto.Phone.Trim();
            
            if (updateEmployeeDto.Role.HasValue)
                employee.Role = updateEmployeeDto.Role.Value;
            
            if (updateEmployeeDto.DepartmentId.HasValue)
                employee.DepartmentId = updateEmployeeDto.DepartmentId.Value;
            
            if (updateEmployeeDto.HiredAt.HasValue)
                employee.HiredAt = updateEmployeeDto.HiredAt.Value;
            
            if (updateEmployeeDto.Active.HasValue)
                employee.Active = updateEmployeeDto.Active.Value;

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
                .Include(e => e.Department)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Email == email.ToLowerInvariant());
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLowerInvariant();
                query = query.Where(e => 
                    e.FirstName.ToLower().Contains(search) ||
                    e.LastName.ToLower().Contains(search) ||
                    e.Email.Contains(search) ||
                    e.EmployeeCode.Contains(search));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }

            if (active.HasValue)
            {
                query = query.Where(e => e.Active == active.Value);
            }

            return await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            // Soft delete
            employee.Active = false;
            employee.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Empleado desactivado: {EmployeeId} - {EmployeeName}", employee.Id, employee.FullName);
            return true;
        }

        public async Task<bool> ActivateEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            employee.Active = true;
            employee.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Empleado activado: {EmployeeId} - {EmployeeName}", employee.Id, employee.FullName);
            return true;
        }

        public async Task<bool> DeactivateEmployeeAsync(int id)
        {
            return await DeleteEmployeeAsync(id);
        }

        public async Task<bool> ChangePasswordAsync(int id, string newPassword)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            employee.PasswordHash = _passwordService.HashPassword(newPassword);
            employee.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Contraseña cambiada para empleado: {EmployeeId}", employee.Id);
            return true;
        }

        public async Task<bool> ValidateEmployeeCredentialsAsync(string email, string password)
        {
            var employee = await GetEmployeeByEmailAsync(email);
            if (employee == null || !employee.Active)
                return false;

            return _passwordService.VerifyPassword(password, employee.PasswordHash);
        }

        public async Task<Employee?> AuthenticateEmployeeAsync(string email, string password)
        {
            var employee = await GetEmployeeByEmailAsync(email);
            if (employee == null || !employee.Active)
                return null;

            if (!_passwordService.VerifyPassword(password, employee.PasswordHash))
                return null;

            // Actualizar último login
            employee.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Empleado autenticado: {EmployeeId} - {Email}", employee.Id, employee.Email);
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
            var query = _context.Employees.Where(e => e.Email == email.ToLowerInvariant());
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId.Value);
            }
            
            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, int? excludeId = null)
        {
            var query = _context.Employees.Where(e => e.EmployeeCode == employeeCode);
            
            if (excludeId.HasValue)
            {
                query = query.Where(e => e.Id != excludeId.Value);
            }
            
            return !await query.AnyAsync();
        }

        public async Task<string> GenerateUniqueEmployeeCodeAsync()
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Active);
            var companyPrefix = company?.Name?.Substring(0, Math.Min(3, company.Name.Length)).ToUpperInvariant() ?? "EMP";
            
            var lastEmployee = await _context.Employees
                .Where(e => e.EmployeeCode.StartsWith(companyPrefix))
                .OrderByDescending(e => e.EmployeeCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEmployee != null)
            {
                // Extraer el número del último código
                var numberPart = lastEmployee.EmployeeCode.Substring(companyPrefix.Length);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            string employeeCode;
            do
            {
                employeeCode = $"{companyPrefix}{nextNumber:D4}";
                nextNumber++;
            }
            while (!await IsEmployeeCodeUniqueAsync(employeeCode));

            return employeeCode;
        }
    }
}