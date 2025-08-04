using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.Core;
using Shared.Models.Enums;
using Shared.Services.Security;

namespace Database.Setup.Tools
{
    public class DatabaseSeeder
    {
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly IPasswordService _passwordService;

        public DatabaseSeeder(ILogger<DatabaseSeeder> logger, IPasswordService passwordService)
        {
            _logger = logger;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Sembrar datos iniciales en la base de datos central de Sphere
        /// </summary>
        public async Task SeedSphereDataAsync(DbContext context)
        {
            try
            {
                _logger.LogInformation("Iniciando siembra de datos para Sphere Admin");

                // Crear super administrador si no existe
                await CreateSuperAdminAsync(context);

                // Crear empresa demo si no existe
                await CreateDemoCompanyAsync(context);

                await context.SaveChangesAsync();
                _logger.LogInformation("Siembra de datos completada para Sphere Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sembrando datos para Sphere Admin");
                throw;
            }
        }

        /// <summary>
        /// Sembrar datos iniciales para una empresa (tenant)
        /// </summary>
        public async Task SeedCompanyDataAsync(DbContext context, int companyId, string companyName)
        {
            try
            {
                _logger.LogInformation("Iniciando siembra de datos para empresa {CompanyName}", companyName);

                // Crear departamentos por defecto
                await CreateDefaultDepartmentsAsync(context, companyId);

                // Crear empleado administrador
                await CreateCompanyAdminAsync(context, companyId);

                // Crear empleados demo
                await CreateDemoEmployeesAsync(context, companyId);

                await context.SaveChangesAsync();
                _logger.LogInformation("Siembra de datos completada para empresa {CompanyName}", companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sembrando datos para empresa {CompanyName}", companyName);
                throw;
            }
        }

        /// <summary>
        /// Crear super administrador del sistema
        /// </summary>
        private async Task CreateSuperAdminAsync(DbContext context)
        {
            var adminEmail = "admin@spheretime.com";
            
            // Verificar si ya existe
            var existingAdmin = await context.Set<SphereAdmin>()
                .FirstOrDefaultAsync(sa => sa.Email == adminEmail);

            if (existingAdmin == null)
            {
                var superAdmin = new SphereAdmin
                {
                    FirstName = "Super",
                    LastName = "Administrador",
                    Email = adminEmail,
                    PasswordHash = _passwordService.HashPassword("SphereAdmin123!"),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Set<SphereAdmin>().Add(superAdmin);
                _logger.LogInformation("Super administrador creado: {Email}", adminEmail);
            }
        }

        /// <summary>
        /// Crear empresa demo
        /// </summary>
        private async Task CreateDemoCompanyAsync(DbContext context)
        {
            var companyName = "Empresa Demo";
            
            // Verificar si ya existe
            var existingCompany = await context.Set<Company>()
                .FirstOrDefaultAsync(c => c.Name == companyName);

            if (existingCompany == null)
            {
                var demoCompany = new Company
                {
                    Name = companyName,
                    Email = "contacto@empresademo.com",
                    Phone = "+34 123 456 789",
                    Address = "Calle Demo 123, 28001 Madrid",
                    TaxId = "B12345678",
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Set<Company>().Add(demoCompany);
                _logger.LogInformation("Empresa demo creada: {Name}", companyName);
            }
        }

        /// <summary>
        /// Crear departamentos por defecto para una empresa
        /// </summary>
        private async Task CreateDefaultDepartmentsAsync(DbContext context, int companyId)
        {
            var defaultDepartments = new[]
            {
                new { Name = "Recursos Humanos", Description = "Gestión de personal y recursos humanos" },
                new { Name = "Desarrollo", Description = "Equipo de desarrollo de software" },
                new { Name = "Marketing", Description = "Marketing y comunicación" },
                new { Name = "Ventas", Description = "Equipo comercial y ventas" },
                new { Name = "Administración", Description = "Administración y finanzas" }
            };

            foreach (var deptInfo in defaultDepartments)
            {
                // Verificar si ya existe
                var existingDept = await context.Set<Department>()
                    .FirstOrDefaultAsync(d => d.Name == deptInfo.Name && d.CompanyId == companyId);

                if (existingDept == null)
                {
                    var department = new Department
                    {
                        Name = deptInfo.Name,
                        Description = deptInfo.Description,
                        CompanyId = companyId,
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Set<Department>().Add(department);
                    _logger.LogInformation("Departamento creado: {Name} para empresa {CompanyId}", deptInfo.Name, companyId);
                }
            }
        }

        /// <summary>
        /// Crear administrador de la empresa
        /// </summary>
        private async Task CreateCompanyAdminAsync(DbContext context, int companyId)
        {
            var adminEmail = $"admin@company{companyId}.com";
            
            // Verificar si ya existe
            var existingAdmin = await context.Set<Employee>()
                .FirstOrDefaultAsync(e => e.Email == adminEmail);

            if (existingAdmin == null)
            {
                // Obtener departamento de Administración
                var adminDept = await context.Set<Department>()
                    .FirstOrDefaultAsync(d => d.Name == "Administración" && d.CompanyId == companyId);

                var admin = new Employee
                {
                    FirstName = "Administrador",
                    LastName = "Empresa",
                    Email = adminEmail,
                    EmployeeCode = "ADM001",
                    Phone = "+34 600 000 001",
                    CompanyId = companyId,
                    DepartmentId = adminDept?.Id,
                    Position = "Administrador",
                    Role = UserRole.Admin,
                    HireDate = DateTime.Today,
                    Salary = 50000,
                    PasswordHash = _passwordService.HashPassword("Admin123!"),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Set<Employee>().Add(admin);
                _logger.LogInformation("Administrador de empresa creado: {Email}", adminEmail);
            }
        }

        /// <summary>
        /// Crear empleados demo
        /// </summary>
        private async Task CreateDemoEmployeesAsync(DbContext context, int companyId)
        {
            var demoEmployees = new[]
            {
                new { FirstName = "Juan", LastName = "Pérez", Email = "juan.perez@company.com", Department = "Desarrollo", Position = "Desarrollador Senior", Role = UserRole.Employee },
                new { FirstName = "María", LastName = "García", Email = "maria.garcia@company.com", Department = "Recursos Humanos", Position = "HR Manager", Role = UserRole.Supervisor },
                new { FirstName = "Carlos", LastName = "López", Email = "carlos.lopez@company.com", Department = "Marketing", Position = "Marketing Specialist", Role = UserRole.Employee },
                new { FirstName = "Ana", LastName = "Rodríguez", Email = "ana.rodriguez@company.com", Department = "Ventas", Position = "Sales Representative", Role = UserRole.Employee },
                new { FirstName = "Luis", LastName = "Martínez", Email = "luis.martinez@company.com", Department = "Desarrollo", Position = "Team Lead", Role = UserRole.Supervisor }
            };

            var departments = await context.Set<Department>()
                .Where(d => d.CompanyId == companyId)
                .ToListAsync();

            int employeeCounter = 2; // Empezar después del admin (ADM001)

            foreach (var empInfo in demoEmployees)
            {
                // Verificar si ya existe
                var existingEmployee = await context.Set<Employee>()
                    .FirstOrDefaultAsync(e => e.Email == empInfo.Email);

                if (existingEmployee == null)
                {
                    var department = departments.FirstOrDefault(d => d.Name == empInfo.Department);

                    var employee = new Employee
                    {
                        FirstName = empInfo.FirstName,
                        LastName = empInfo.LastName,
                        Email = empInfo.Email,
                        EmployeeCode = $"EMP{employeeCounter:D3}",
                        Phone = $"+34 600 000 {employeeCounter:D3}",
                        CompanyId = companyId,
                        DepartmentId = department?.Id,
                        Position = empInfo.Position,
                        Role = empInfo.Role,
                        HireDate = DateTime.Today.AddDays(-Random.Shared.Next(30, 365)),
                        Salary = Random.Shared.Next(25000, 45000),
                        PasswordHash = _passwordService.HashPassword("Employee123!"),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Set<Employee>().Add(employee);
                    _logger.LogInformation("Empleado demo creado: {Email}", empInfo.Email);
                    
                    employeeCounter++;
                }
            }
        }

        /// <summary>
        /// Limpiar todos los datos de la base de datos
        /// </summary>
        public async Task CleanDatabaseAsync(DbContext context)
        {
            try
            {
                _logger.LogWarning("ATENCIÓN: Limpiando toda la base de datos");

                // Eliminar en orden inverso a las dependencias
                context.Set<VacationRequest>().RemoveRange(context.Set<VacationRequest>());
                context.Set<VacationBalance>().RemoveRange(context.Set<VacationBalance>());
                context.Set<VacationPolicy>().RemoveRange(context.Set<VacationPolicy>());
                context.Set<TimeRecord>().RemoveRange(context.Set<TimeRecord>());
                context.Set<WorkSchedule>().RemoveRange(context.Set<WorkSchedule>());
                context.Set<Employee>().RemoveRange(context.Set<Employee>());
                context.Set<Department>().RemoveRange(context.Set<Department>());
                context.Set<Company>().RemoveRange(context.Set<Company>());
                context.Set<SphereAdmin>().RemoveRange(context.Set<SphereAdmin>());

                await context.SaveChangesAsync();
                _logger.LogWarning("Base de datos limpiada completamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando la base de datos");
                throw;
            }
        }
    }
}