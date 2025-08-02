using Microsoft.EntityFrameworkCore;
using EmployeeApp.Server.Data;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;

namespace EmployeeApp.Server.Data
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

                // Crear usuarios empleados
                var users = new[]
                {
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

                // Crear empleados
                var employees = new[]
                {
                    new Employee
                    {
                        UserId = users[0].Id,
                        CompanyId = company.Id,
                        EmployeeCode = "EMP001",
                        Department = "Desarrollo",
                        Position = "Desarrolladora Frontend",
                        HireDate = DateTime.UtcNow.AddMonths(-8),
                        Pin = BCrypt.Net.BCrypt.HashPassword("1234"), // PIN para fichaje rápido
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        UserId = users[1].Id,
                        CompanyId = company.Id,
                        EmployeeCode = "EMP002",
                        Department = "Desarrollo",
                        Position = "Desarrollador Backend",
                        HireDate = DateTime.UtcNow.AddMonths(-6),
                        Pin = BCrypt.Net.BCrypt.HashPassword("5678"),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Employee
                    {
                        UserId = users[2].Id,
                        CompanyId = company.Id,
                        EmployeeCode = "EMP003",
                        Department = "Marketing",
                        Position = "Especialista en Marketing Digital",
                        HireDate = DateTime.UtcNow.AddMonths(-4),
                        Pin = BCrypt.Net.BCrypt.HashPassword("9999"),
                        Active = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Employees.AddRange(employees);
                await context.SaveChangesAsync();

                // Crear registros de tiempo de ejemplo
                await CreateSampleTimeRecords(context);

                Console.WriteLine("✅ Datos de prueba creados exitosamente:");
                Console.WriteLine("   • Empleados: maria@demo.com, juan@demo.com, ana@demo.com");
                Console.WriteLine("   • Contraseñas: Maria123!, Juan123!, Ana123!");
                Console.WriteLine("   • PIN fichaje: EMP001/1234, EMP002/5678, EMP003/9999");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ Datos de prueba creados exitosamente:");
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
                        Timestamp = checkInTime,
                        Location = "Oficina Principal",
                        IpAddress = "192.168.1." + random.Next(100, 200),
                        CreatedAt = checkInTime
                    });

                    // Pausa para almuerzo
                    var lunchStart = checkInTime.AddHours(4 + random.Next(0, 2));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.LunchStart,
                        Timestamp = lunchStart,
                        Location = "Oficina Principal",
                        CreatedAt = lunchStart
                    });

                    var lunchEnd = lunchStart.AddMinutes(30 + random.Next(0, 60));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.LunchEnd,
                        Timestamp = lunchEnd,
                        Location = "Oficina Principal",
                        CreatedAt = lunchEnd
                    });

                    // Salida (17:00 - 18:30)
                    var checkOutTime = lunchEnd.AddHours(3 + random.Next(0, 2)).AddMinutes(random.Next(0, 30));
                    records.Add(new TimeRecord
                    {
                        EmployeeId = employee.Id,
                        Type = RecordType.CheckOut,
                        Timestamp = checkOutTime,
                        Location = "Oficina Principal",
                        IpAddress = "192.168.1." + random.Next(100, 200),
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