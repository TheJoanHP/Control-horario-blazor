using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Enums;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeRecordsController : ControllerBase
    {
        private readonly CompanyDbContext _context;

        public TimeRecordsController(CompanyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetTimeRecords(
            [FromQuery] int? employeeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                        .ThenInclude(e => e.User)
                    .Where(tr => tr.Timestamp >= fromDate && tr.Timestamp <= toDate);

                if (employeeId.HasValue)
                {
                    query = query.Where(tr => tr.EmployeeId == employeeId.Value);
                }

                var totalRecords = await query.CountAsync();
                var records = await query
                    .OrderByDescending(tr => tr.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(tr => new
                    {
                        tr.Id,
                        tr.Type,
                        tr.Timestamp,
                        tr.Notes,
                        tr.Location,
                        tr.IpAddress,
                        Employee = new
                        {
                            tr.Employee.Id,
                            tr.Employee.User.Name,
                            tr.Employee.EmployeeCode,
                            tr.Employee.Department
                        },
                        FormattedTimestamp = tr.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                        TypeName = tr.Type.ToString()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    records,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalRecords,
                        totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                    },
                    filters = new
                    {
                        employeeId,
                        from = fromDate,
                        to = toDate
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo registros de tiempo" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportTimeRecords(
            [FromQuery] int? employeeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string format = "csv")
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                        .ThenInclude(e => e.User)
                    .Where(tr => tr.Timestamp >= fromDate && tr.Timestamp <= toDate);

                if (employeeId.HasValue)
                {
                    query = query.Where(tr => tr.EmployeeId == employeeId.Value);
                }

                var records = await query
                    .OrderByDescending(tr => tr.Timestamp)
                    .Select(tr => new
                    {
                        Empleado = tr.Employee.User.Name,
                        Codigo = tr.Employee.EmployeeCode,
                        Departamento = tr.Employee.Department,
                        Tipo = tr.Type.ToString(),
                        Fecha = tr.Timestamp.ToString("dd/MM/yyyy"),
                        Hora = tr.Timestamp.ToString("HH:mm:ss"),
                        Ubicacion = tr.Location ?? "",
                        Notas = tr.Notes ?? "",
                        IP = tr.IpAddress ?? ""
                    })
                    .ToListAsync();

                if (format.ToLower() == "csv")
                {
                    var csv = GenerateCSV(records);
                    var fileName = $"registros_tiempo_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv";
                    
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), 
                               "text/csv", 
                               fileName);
                }

                return Ok(new
                {
                    success = true,
                    data = records,
                    count = records.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error exportando registros" });
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetTimeRecordsSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                var records = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                        .ThenInclude(e => e.User)
                    .Where(tr => tr.Timestamp >= fromDate && tr.Timestamp <= toDate)
                    .ToListAsync();

                // Resumen por empleado
                var employeeSummary = records
                    .GroupBy(tr => new { tr.EmployeeId, tr.Employee.User.Name, tr.Employee.EmployeeCode })
                    .Select(g => new
                    {
                        employeeId = g.Key.EmployeeId,
                        employeeName = g.Key.Name,
                        employeeCode = g.Key.EmployeeCode,
                        totalRecords = g.Count(),
                        checkIns = g.Count(tr => tr.Type == RecordType.CheckIn),
                        checkOuts = g.Count(tr => tr.Type == RecordType.CheckOut),
                        totalHours = CalculateEmployeeHours(g.ToList()),
                        daysWorked = g.Where(tr => tr.Type == RecordType.CheckIn)
                                     .Select(tr => tr.Timestamp.Date)
                                     .Distinct()
                                     .Count(),
                        avgHoursPerDay = 0.0 // Se calculará después
                    })
                    .ToList();

                // Calcular promedio de horas por día
                foreach (var emp in employeeSummary)
                {
                    if (emp.daysWorked > 0)
                    {
                        emp.GetType().GetProperty("avgHoursPerDay")?.SetValue(emp, Math.Round(emp.totalHours / emp.daysWorked, 2));
                    }
                }

                // Resumen general
                var totalHours = employeeSummary.Sum(e => e.totalHours);
                var totalDays = employeeSummary.Sum(e => e.daysWorked);

                return Ok(new
                {
                    success = true,
                    period = new
                    {
                        from = fromDate.ToString("dd/MM/yyyy"),
                        to = toDate.ToString("dd/MM/yyyy"),
                        days = (toDate - fromDate).Days + 1
                    },
                    summary = new
                    {
                        totalEmployees = employeeSummary.Count,
                        totalRecords = records.Count,
                        totalHours = Math.Round(totalHours, 2),
                        totalDaysWorked = totalDays,
                        avgHoursPerEmployee = employeeSummary.Count > 0 ? Math.Round(totalHours / employeeSummary.Count, 2) : 0
                    },
                    employees = employeeSummary.OrderByDescending(e => e.totalHours)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo resumen de registros" });
            }
        }

        private double CalculateEmployeeHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var groupedByDay = records.GroupBy(r => r.Timestamp.Date);
            double totalHours = 0;

            foreach (var dayGroup in groupedByDay)
            {
                var dayRecords = dayGroup.OrderBy(r => r.Timestamp).ToList();
                Shared.Models.TimeTracking.TimeRecord? lastCheckIn = null;

                foreach (var record in dayRecords)
                {
                    if (record.Type == RecordType.CheckIn)
                        lastCheckIn = record;
                    else if (record.Type == RecordType.CheckOut && lastCheckIn != null)
                    {
                        totalHours += (record.Timestamp - lastCheckIn.Timestamp).TotalHours;
                        lastCheckIn = null;
                    }
                }
            }

            return Math.Round(totalHours, 2);
        }

        private string GenerateCSV<T>(IEnumerable<T> records)
        {
            var csv = new System.Text.StringBuilder();
            
            if (records.Any())
            {
                // Headers
                var properties = typeof(T).GetProperties();
                csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

                // Data
                foreach (var record in records)
                {
                    var values = properties.Select(p => 
                    {
                        var value = p.GetValue(record)?.ToString() ?? "";
                        // Escapar comillas y comas
                        if (value.Contains(",") || value.Contains("\""))
                        {
                            value = "\"" + value.Replace("\"", "\"\"") + "\"";
                        }
                        return value;
                    });
                    csv.AppendLine(string.Join(",", values));
                }
            }

            return csv.ToString();
        }
    }
}