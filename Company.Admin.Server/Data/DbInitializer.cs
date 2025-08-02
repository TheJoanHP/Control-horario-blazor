using Company.Admin.Server.Data;
using Shared.Models.Core;

namespace Company.Admin.Server.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(CompanyDbContext context)
        {
            try
            {
                // Asegurar que la base de datos existe
                await context.Database.EnsureCreatedAsync();

                // Si ya hay datos, no hacer nada
                if (context.Companies.Any())
                {
                    Console.WriteLine("✅ Base de datos ya tiene datos");
                    return;
                }

                Console.WriteLine("🔄 Creando datos de prueba...");

                // Crear empresa de prueba
                var company = new Shared.Models.Core.Company
                {
                    Name = "Demo Company",
                    TaxId = "12345678A",
                    Address = "Calle Demo, 123",
                    Phone = "+34 123 456 789",
                    Email = "info@democompany.com",
                    Active = true,
                    WorkStartTime = new TimeSpan(9, 0, 0),
                    WorkEndTime = new TimeSpan(17, 0, 0),
                    ToleranceMinutes = 15,
                    CreatedAt = DateTime.UtcNow
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();

                // Crear departamentos
                var departments = new[]
                {
                    new Department { CompanyId = company.Id, Name = "Administración", Description = "Departamento administrativo", Active = true, CreatedAt = DateTime.UtcNow },
                    new Department { CompanyId = company.Id, Name = "Desarrollo", Description = "Departamento de desarrollo", Active = true, CreatedAt = DateTime.UtcNow },
                    new Department { CompanyId = company.Id, Name = "Marketing", Description = "Departamento de marketing", Active = true, CreatedAt = DateTime.UtcNow }
                };

                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();

                // Crear usuarios de prueba
                var users = new[]
                {
                    // Admin de empresa
                    new User
                    {
                        Name = "Admin Demo",
                        Email = "admin@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role = "COMPANY_ADMIN",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    // Supervisor
                    new User
                    {
                        Name = "Supervisor Demo",
                        Email = "supervisor@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Super123!"),
                        Role = "SUPERVISOR",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    // Empleados
                    new User
                    {
                        Name = "María García",
                        Email = "maria@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Maria123!"),
                        Role = "EMPLOYEE",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Name = "Juan López",
                        Email = "juan@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Juan123!"),
                        Role = "EMPLOYEE",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Name = "Ana Martín",
                        Email = "ana@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Ana123!"),
                        Role = "EMPLOYEE",
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Users.AddRange(users);
                await context.SaveChangesAsync();

                // Crear empleados asociados
                var employees = new[]
                {
                    new Employee
                    {
                        UserId = users[0].Id, // Admin
                        CompanyId = company.Id,
                        EmployeeCode = "ADM001",
                        Department = "Administración",
                        Position = "Administrador General",
                        HireDate = DateTime.UtcNow.AddYears(-2),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        UserId = users[1].Id, // Supervisor
                        CompanyId = company.Id,
                        EmployeeCode = "SUP001",
                        Department = "Desarrollo",
                        Position = "Supervisor de Desarrollo",
                        HireDate = DateTime.UtcNow.AddYears(-1),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        UserId = users[2].Id, // María
                        CompanyId = company.Id,
                        EmployeeCode = "EMP001",
                        Department = "Desarrollo",
                        Position = "Desarrolladora Frontend",
                        HireDate = DateTime.UtcNow.AddMonths(-8),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        UserId = users[3].Id, // Juan
                        CompanyId = company.Id,
                        EmployeeCode = "EMP002",
                        Department = "Desarrollo",
                        Position = "Desarrollador Backend",
                        HireDate = DateTime.UtcNow.AddMonths(-6),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        UserId = users[4].Id, // Ana
                        CompanyId = company.Id,
                        EmployeeCode = "EMP003",
                        Department = "Marketing",
                        Position = "Especialista en Marketing Digital",
                        HireDate = DateTime.UtcNow.AddMonths(-4),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Employees.AddRange(employees);
                await context.SaveChangesAsync();

                Console.WriteLine("✅ Datos de prueba creados exitosamente:");
                Console.WriteLine("   • Empresa: Demo Company");
                Console.WriteLine("   • Admin: admin@demo.com / Admin123!");
                Console.WriteLine("   • Supervisor: supervisor@demo.com / Super123!");
                Console.WriteLine("   • Empleados: maria@demo.com, juan@demo.com, ana@demo.com");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creando datos de prueba: {ex.Message}");
            }
        }
    }
}