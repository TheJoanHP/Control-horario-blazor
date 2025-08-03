using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.Enums;
using Shared.Services.Security;

namespace Company.Admin.Server.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(CompanyDbContext context, IPasswordService passwordService)
        {
            try
            {
                // Crear la base de datos si no existe
                await context.Database.EnsureCreatedAsync();

                // Si ya hay datos, salir
                if (context.Companies.Any())
                    return;

                // Crear empresa de demo
                var company = new Company
                {
                    Name = "Empresa Demo",
                    TaxId = "12345678-A",
                    Address = "Calle Demo 123, Ciudad Demo",
                    Phone = "+34 123 456 789",
                    Email = "demo@empresa.com",
                    Active = true,
                    WorkStartTime = new TimeSpan(9, 0, 0),
                    WorkEndTime = new TimeSpan(17, 0, 0),
                    ToleranceMinutes = 15,
                    VacationDaysPerYear = 22,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();

                // Crear departamentos
                var departments = new[]
                {
                    new Department
                    {
                        CompanyId = company.Id,
                        Name = "Recursos Humanos",
                        Description = "Departamento de gestión de personal",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Department
                    {
                        CompanyId = company.Id,
                        Name = "Desarrollo",
                        Description = "Departamento de desarrollo de software",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Department
                    {
                        CompanyId = company.Id,
                        Name = "Marketing",
                        Description = "Departamento de marketing y ventas",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Department
                    {
                        CompanyId = company.Id,
                        Name = "Finanzas",
                        Description = "Departamento de administración y finanzas",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();

                // Crear empleados de demo
                var employees = new List<Employee>();

                // Admin de la empresa
                var adminEmployee = new Employee
                {
                    CompanyId = company.Id,
                    DepartmentId = departments[0].Id, // Recursos Humanos
                    FirstName = "Admin",
                    LastName = "Empresa",
                    Email = "admin@empresa.com",
                    Phone = "+34 123 456 700",
                    EmployeeCode = "EMP001",
                    Role = UserRole.CompanyAdmin,
                    PasswordHash = passwordService.HashPassword("admin123"),
                    Active = true,
                    HiredAt = DateTime.UtcNow.AddYears(-2),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                employees.Add(adminEmployee);

                // Empleado de Desarrollo
                var devEmployee = new Employee
                {
                    CompanyId = company.Id,
                    DepartmentId = departments[1].Id, // Desarrollo
                    FirstName = "Juan",
                    LastName = "Pérez",
                    Email = "juan.perez@empresa.com",
                    Phone = "+34 123 456 701",
                    EmployeeCode = "EMP002",
                    Role = UserRole.Employee,
                    PasswordHash = passwordService.HashPassword("empleado123"),
                    Active = true,
                    HiredAt = DateTime.UtcNow.AddYears(-1),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                employees.Add(devEmployee);

                // Empleado de Marketing
                var marketingEmployee = new Employee
                {
                    CompanyId = company.Id,
                    DepartmentId = departments[2].Id, // Marketing
                    FirstName = "María",
                    LastName = "García",
                    Email = "maria.garcia@empresa.com",
                    Phone = "+34 123 456 702",
                    EmployeeCode = "EMP003",
                    Role = UserRole.Employee,
                    PasswordHash = passwordService.HashPassword("empleado123"),
                    Active = true,
                    HiredAt = DateTime.UtcNow.AddMonths(-8),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                employees.Add(marketingEmployee);

                // Empleado de Finanzas
                var financeEmployee = new Employee
                {
                    CompanyId = company.Id,
                    DepartmentId = departments[3].Id, // Finanzas
                    FirstName = "Carlos",
                    LastName = "López",
                    Email = "carlos.lopez@empresa.com",
                    Phone = "+34 123 456 703",
                    EmployeeCode = "EMP004",
                    Role = UserRole.Employee,
                    PasswordHash = passwordService.HashPassword("empleado123"),
                    Active = true,
                    HiredAt = DateTime.UtcNow.AddMonths(-6),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                employees.Add(financeEmployee);

                // Supervisor de Desarrollo
                var supervisorEmployee = new Employee
                {
                    CompanyId = company.Id,
                    DepartmentId = departments[1].Id, // Desarrollo
                    FirstName = "Ana",
                    LastName = "Martínez",
                    Email = "ana.martinez@empresa.com",
                    Phone = "+34 123 456 704",
                    EmployeeCode = "EMP005",
                    Role = UserRole.Supervisor,
                    PasswordHash = passwordService.HashPassword("supervisor123"),
                    Active = true,
                    HiredAt = DateTime.UtcNow.AddYears(-3),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                employees.Add(supervisorEmployee);

                context.Employees.AddRange(employees);
                await context.SaveChangesAsync();

                Console.WriteLine("Base de datos inicializada correctamente con datos de demo.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar la base de datos: {ex.Message}");
                throw;
            }
        }
    }
}