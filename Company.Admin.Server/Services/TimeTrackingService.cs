using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Company.Admin.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.Enums;

namespace Company.Admin.Server.Services
{
    public class TimeTrackingService : ITimeTrackingService
    {
        private readonly CompanyDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TimeTrackingService> _logger;

        public TimeTrackingService(
            CompanyDbContext context,
            IMapper mapper,
            ILogger<TimeTrackingService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TimeRecord> CreateTimeRecordAsync(int employeeId, RecordType type, CheckInDto checkInDto)
        {
            try
            {
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employeeId,
                    Type = type,
                    Date = checkInDto.Date ?? DateTime.Today,
                    Time = checkInDto.Time ?? DateTime.Now,
                    Location = checkInDto.Location,
                    Notes = checkInDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Registro de tiempo creado: {RecordId} - Empleado {EmployeeId} - Tipo {Type}", 
                    timeRecord.Id, employeeId, type);

                return timeRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando registro de tiempo para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecord> CheckInAsync(int employeeId, CheckInDto checkInDto)
        {
            try
            {
                // Verificar que el empleado no esté ya fichado
                if (await IsEmployeeCheckedInAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado ya está fichado de entrada");
                }

                return await CreateTimeRecordAsync(employeeId, RecordType.CheckIn, checkInDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en check-in para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecord> CheckOutAsync(int employeeId, CheckOutDto checkOutDto)
        {
            try
            {
                // Verificar que el empleado esté fichado
                if (!await IsEmployeeCheckedInAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado no está fichado de entrada");
                }

                var checkInDto = new CheckInDto
                {
                    Date = checkOutDto.Date,
                    Time = checkOutDto.Time,
                    Location = checkOutDto.Location,
                    Notes = checkOutDto.Notes
                };

                return await CreateTimeRecordAsync(employeeId, RecordType.CheckOut, checkInDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en check-out para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecord> StartBreakAsync(int employeeId, CheckInDto breakDto)
        {
            try
            {
                // Verificar que el empleado esté fichado y no esté en descanso
                if (!await IsEmployeeCheckedInAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado debe estar fichado para iniciar un descanso");
                }

                if (await IsEmployeeOnBreakAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado ya está en descanso");
                }

                return await CreateTimeRecordAsync(employeeId, RecordType.BreakStart, breakDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando descanso para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecord> EndBreakAsync(int employeeId, CheckOutDto breakDto)
        {
            try
            {
                // Verificar que el empleado esté en descanso
                if (!await IsEmployeeOnBreakAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado no está en descanso");
                }

                var checkInDto = new CheckInDto
                {
                    Date = breakDto.Date,
                    Time = breakDto.Time,
                    Location = breakDto.Location,
                    Notes = breakDto.Notes
                };

                return await CreateTimeRecordAsync(employeeId, RecordType.BreakEnd, checkInDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizando descanso para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecord?> GetLastOpenRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId &&
                            (tr.Type == RecordType.CheckIn || tr.Type == RecordType.BreakStart))
                .OrderByDescending(tr => tr.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TimeRecord?> GetLastRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId)
                .OrderByDescending(tr => tr.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TimeRecord>> GetTimeRecordsAsync(int? employeeId = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.TimeRecords
                .Include(tr => tr.Employee)
                .AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(tr => tr.EmployeeId == employeeId);
            }

            if (from.HasValue)
            {
                query = query.Where(tr => tr.Date >= from);
            }

            if (to.HasValue)
            {
                query = query.Where(tr => tr.Date <= to);
            }

            return await query
                .OrderByDescending(tr => tr.Date)
                .ThenByDescending(tr => tr.Time)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeRecord>> GetDailyRecordsAsync(int employeeId, DateTime date)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date.Date == date.Date)
                .OrderBy(tr => tr.Time)
                .ToListAsync();
        }

        public async Task<bool> HasOpenRecordAsync(int employeeId)
        {
            var lastRecord = await GetLastRecordAsync(employeeId);
            if (lastRecord == null) return false;

            return lastRecord.Type == RecordType.CheckIn || lastRecord.Type == RecordType.BreakStart;
        }

        public async Task<bool> IsEmployeeCheckedInAsync(int employeeId)
        {
            var today = DateTime.Today;
            var todayRecords = await GetDailyRecordsAsync(employeeId, today);

            if (!todayRecords.Any()) return false;

            var lastRecord = todayRecords.OrderByDescending(r => r.Time).First();
            return lastRecord.Type == RecordType.CheckIn || lastRecord.Type == RecordType.BreakEnd;
        }

        public async Task<bool> IsEmployeeOnBreakAsync(int employeeId)
        {
            var today = DateTime.Today;
            var todayRecords = await GetDailyRecordsAsync(employeeId, today);

            if (!todayRecords.Any()) return false;

            var lastRecord = todayRecords.OrderByDescending(r => r.Time).First();
            return lastRecord.Type == RecordType.BreakStart;
        }

        public async Task<TimeSpan> CalculateWorkedHoursAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            
            // TODO: Implementar lógica de cálculo de horas trabajadas
            // Por ahora, retorna un placeholder
            return TimeSpan.FromHours(8);
        }

        public async Task<TimeSpan> CalculateBreakTimeAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            
            // TODO: Implementar lógica de cálculo de tiempo de descanso
            // Por ahora, retorna un placeholder
            return TimeSpan.FromHours(1);
        }

        public async Task<bool> ValidateCheckInAsync(int employeeId)
        {
            try
            {
                // Verificar que el empleado existe y está activo
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null || !employee.Active)
                {
                    return false;
                }

                // Verificar que no esté ya fichado
                return !await IsEmployeeCheckedInAsync(employeeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando check-in para empleado {EmployeeId}", employeeId);
                return false;
            }
        }

        public async Task<bool> ValidateCheckOutAsync(int employeeId)
        {
            try
            {
                // Verificar que el empleado existe y está activo
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null || !employee.Active)
                {
                    return false;
                }

                // Verificar que esté fichado
                return await IsEmployeeCheckedInAsync(employeeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando check-out para empleado {EmployeeId}", employeeId);
                return false;
            }
        }

        public async Task<string> GetEmployeeStatusAsync(int employeeId)
        {
            try
            {
                if (await IsEmployeeOnBreakAsync(employeeId))
                {
                    return "En descanso";
                }
                else if (await IsEmployeeCheckedInAsync(employeeId))
                {
                    return "Trabajando";
                }
                else
                {
                    return "Fuera de trabajo";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado del empleado {EmployeeId}", employeeId);
                return "Desconocido";
            }
        }
    }
}