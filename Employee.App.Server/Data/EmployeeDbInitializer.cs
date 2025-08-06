using Microsoft.EntityFrameworkCore;
using Employee.App.Server.Data; // Corregido namespace
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;

namespace Employee.App.Server.Data // Corregido namespace
{
    public static class EmployeeDbInitializer
    {
        public static async Task InitializeAsync(EmployeeDbContext context)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();

                if (context.Companies.Any())
                {
                    Console.WriteLine("✅ Base de datos ya tiene datos");
                    await CreateSampleTimeRecords(context);
                    return;
                }

                Console.WriteLine("🔄 Creando datos de prueba para Employee.App...");

                // Crear empresa
                var company = new Shared.Models.Core.Company
                {
                    Name = "Demo Company",
                    Code = "DEMO",
                    Email = "info@democompany.com",
                    Phone = "+34 123 456 789",
                    Address = "Calle Demo, 123",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();

                // Crear usuarios empleados
                var users = new[]
                {
                    new User
                    {
                        FirstName = "María",
                        LastName = "García",
                        Email = "maria@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Maria123!"),
                        Role = UserRole.Employee,
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Juan",
                        LastName = "López",
                        Email = "juan@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Juan123!"),
                        Role = UserRole.Employee,
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        FirstName = "Ana",
                        LastName = "Martín",
                        Email = "ana@demo.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Ana123!"),
                        Role = UserRole.Employee,
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Users.AddRange(users);
                await context.SaveChangesAsync();

                // Crear empleados
                var employees = new[]
                {
                    new Shared.Models.Core.Employee
                    {
                        UserId = users[0].Id,
                        CompanyId = company.Id,
                        FirstName = users[0].FirstName,
                        LastName = users[0].LastName,
                        Email = users[0].Email,
                        EmployeeCode = "EMP001",
                        Position = "Desarrolladora",
                        Role = UserRole.Employee,
                        PasswordHash = users[0].PasswordHash,
                        HireDate = DateTime.UtcNow.AddMonths(-6),
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Shared.Models.Core.Employee
                    {
                        UserId = users[1].Id,
                        CompanyId = company.Id,
                        FirstName = users[1].FirstName,
                        LastName = users[1].LastName,
                        Email = users[1].Email,
                        EmployeeCode = "EMP002",
                        Position = "Diseñador",
                        Role = UserRole.Employee,
                        PasswordHash = users[1].PasswordHash,
                        HireDate = DateTime.UtcNow.AddMonths(-3),
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Shared.Models.Core.Employee
                    {
                        UserId = users[2].Id,
                        CompanyId = company.Id,
                        FirstName = users[2].FirstName,
                        LastName = users[2].LastName,
                        Email = users[2].Email,
                        EmployeeCode = "EMP003",
                        Position = "Administradora",
                        Role = UserRole.Employee,
                        PasswordHash = users[2].PasswordHash,
                        HireDate = DateTime.UtcNow.AddMonths(-1),
                        Active = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Employees.AddRange(employees);
                await context.SaveChangesAsync();

                await CreateSampleTimeRecords(context);

                Console.WriteLine("✅ Datos de prueba creados exitosamente:");
                Console.WriteLine("   • Empresa: Demo Company");
                Console.WriteLine("   • Usuarios: maria@demo.com, juan@demo.com, ana@demo.com");
                Console.WriteLine("   • Contraseña: Maria123!, Juan123!, Ana123!");
                Console.WriteLine("   • Códigos empleado: EMP001, EMP002, EMP003");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creando datos de prueba: {ex.Message}");
                throw;
            }
        }

        private static async Task CreateSampleTimeRecords(EmployeeDbContext context)
        {
            var employees = await context.Employees.ToListAsync();
            if (!employees.Any()) return;

            // Verificar si ya hay registros
            if (await context.TimeRecords.AnyAsync()) return;

            Console.WriteLine("🔄 Creando registros de tiempo de ejemplo...");

            var random = new Random();
            var records = new List<TimeRecord>();

            // Crear registros para los últimos 7 días
            for (int days = 7; days >= 1; days--)
            {
                var date = DateTime.UtcNow.AddDays(-days).Date;
                
                // Solo días laborables
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                foreach (var employee in employees.Take(2)) // Solo para los primeros 2 empleados
                {
                    // Entrada (8:30 - 9:30)
                    var checkInTime = date.AddHours(8).AddMinutes(30 + random.Next(0, 60));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.CheckIn,
                        Date = checkInTime.Date,
                        Time = checkInTime.TimeOfDay,
                        Timestamp = checkInTime,
                        Location = "Oficina Principal",
                        CreatedAt = checkInTime
                    });

                    // Pausa para almuerzo
                    var lunchStart = checkInTime.AddHours(4 + random.Next(0, 2));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.BreakStart,
                        Date = lunchStart.Date,
                        Time = lunchStart.TimeOfDay,
                        Timestamp = lunchStart,
                        Location = "Oficina Principal",
                        Notes = "Pausa almuerzo",
                        CreatedAt = lunchStart
                    });

                    var lunchEnd = lunchStart.AddMinutes(30 + random.Next(0, 60));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.BreakEnd,
                        Date = lunchEnd.Date,
                        Time = lunchEnd.TimeOfDay,
                        Timestamp = lunchEnd,
                        Location = "Oficina Principal",
                        Notes = "Fin pausa almuerzo",
                        CreatedAt = lunchEnd
                    });

                    // Salida (17:00 - 18:30)
                    var checkOutTime = lunchEnd.AddHours(3 + random.Next(0, 2)).AddMinutes(random.Next(0, 30));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.CheckOut,
                        Date = checkOutTime.Date,
                        Time = checkOutTime.TimeOfDay,
                        Timestamp = checkOutTime,
                        Location = "Oficina Principal",
                        CreatedAt = checkOutTime
                    });
                }
            }

            context.TimeRecords.AddRange(records);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ {records.Count} registros de tiempo creados");
        }
    }
}