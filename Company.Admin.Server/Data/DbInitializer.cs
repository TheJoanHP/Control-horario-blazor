using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.Enums;
using Shared.Services.Security;

namespace Company.Admin.Server.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(CompanyDbContext context, IPasswordService passwordService)
        {
            try
            {
                // Asegurar que la base de datos existe
                await context.Database.EnsureCreatedAsync();

                // Si ya hay datos, no hacer nada
                if (context.Companies.Any())
                {
                    return;
                }

                // Crear empresa demo
                var company = new Shared.Models.Core.Company
                {
                    Name = "Empresa Demo",
                    Subdomain = "demo",
                    Email = "admin@empresademo.com",
                    Phone = "+34 666 777 888",
                    Address = "Calle Principal 123, Madrid",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();

                // Crear departamento por defecto
                var department = new Department
                {
                    CompanyId = company.Id,
                    Name = "Administración",
                    Description = "Departamento administrativo",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Departments.Add(department);
                await context.SaveChangesAsync();

                // Crear usuario administrador
                var adminUser = new User
                {
                    CompanyId = company.Id,
                    Username = "admin",
                    Email = "admin@empresademo.com",
                    PasswordHash = passwordService.HashPassword("admin123"),
                    FirstName = "Administrador",
                    LastName = "Sistema",
                    Role = UserRole.CompanyAdmin,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                // Crear empleado para el admin
                var adminEmployee = new Employee
                {
                    UserId = adminUser.Id,
                    CompanyId = company.Id,
                    DepartmentId = department.Id,
                    EmployeeNumber = "EMP001",
                    Position = "Administrador del Sistema",
                    HireDate = DateTime.Today,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Employees.Add(adminEmployee);

                // Crear horario de trabajo por defecto
                var defaultSchedule = new Shared.Models.TimeTracking.WorkSchedule
                {
                    Name = "Horario Estándar",
                    Description = "Horario de trabajo estándar de oficina",
                    MondayEnabled = true,
                    MondayStart = new TimeSpan(9, 0, 0),
                    MondayEnd = new TimeSpan(17, 0, 0),
                    TuesdayEnabled = true,
                    TuesdayStart = new TimeSpan(9, 0, 0),
                    TuesdayEnd = new TimeSpan(17, 0, 0),
                    WednesdayEnabled = true,
                    WednesdayStart = new TimeSpan(9, 0, 0),
                    WednesdayEnd = new TimeSpan(17, 0, 0),
                    ThursdayEnabled = true,
                    ThursdayStart = new TimeSpan(9, 0, 0),
                    ThursdayEnd = new TimeSpan(17, 0, 0),
                    FridayEnabled = true,
                    FridayStart = new TimeSpan(9, 0, 0),
                    FridayEnd = new TimeSpan(17, 0, 0),
                    SaturdayEnabled = false,
                    SundayEnabled = false,
                    BreakDuration = new TimeSpan(1, 0, 0),
                    FlexibleHours = false,
                    MaxFlexMinutes = 30,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.WorkSchedules.Add(defaultSchedule);

                // Crear empleados de ejemplo
                var employees = new List<Employee>();
                var users = new List<User>();

                for (int i = 1; i <= 5; i++)
                {
                    var user = new User
                    {
                        CompanyId = company.Id,
                        Username = $"empleado{i}",
                        Email = $"empleado{i}@empresademo.com",
                        PasswordHash = passwordService.HashPassword("empleado123"),
                        FirstName = $"Empleado",
                        LastName = $"Número {i}",
                        Role = UserRole.Employee,
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    users.Add(user);
                }

                context.Users.AddRange(users);
                await context.SaveChangesAsync();

                for (int i = 0; i < users.Count; i++)
                {
                    var employee = new Employee
                    {
                        UserId = users[i].Id,
                        CompanyId = company.Id,
                        DepartmentId = department.Id,
                        EmployeeNumber = $"EMP{(i + 2):D3}",
                        Position = i % 2 == 0 ? "Desarrollador" : "Analista",
                        HireDate = DateTime.Today.AddDays(-30 * (i + 1)),
                        WorkScheduleId = defaultSchedule.Id,
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    employees.Add(employee);
                }

                context.Employees.AddRange(employees);

                // Crear política de vacaciones por defecto
                var vacationPolicy = new Shared.Models.Vacations.VacationPolicy
                {
                    CompanyId = company.Id,
                    Name = "Política Estándar",
                    DaysPerYear = 22,
                    MaxCarryOver = 5,
                    RequireApproval = true,
                    MinAdvanceNoticeDays = 7,
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.VacationPolicies.Add(vacationPolicy);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar la base de datos: {ex.Message}", ex);
            }
        }
    }
}