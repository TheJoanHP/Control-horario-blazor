using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.Enums;

namespace Company.Admin.Server.Services
{
    public class TimeTrackingService : ITimeTrackingService
    {
        private readonly CompanyDbContext _context;
        private readonly ILogger<TimeTrackingService> _logger;

        public TimeTrackingService(CompanyDbContext context, ILogger<TimeTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TimeRecord> CreateTimeRecordAsync(int employeeId, RecordType type, CheckInDto checkInDto)
        {
            var timeRecord = new TimeRecord
            {
                EmployeeId = employeeId,
                Type = type,
                Timestamp = checkInDto.Timestamp ?? DateTime.UtcNow,
                Notes = checkInDto.Notes,
                Latitude = checkInDto.Latitude,
                Longitude = checkInDto.Longitude,
                Location = checkInDto.Location,
                DeviceInfo = checkInDto.DeviceInfo,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimeRecords.Add(timeRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registro de tiempo creado: {Type} para empleado {EmployeeId} a las {Timestamp}", 
                type, employeeId, timeRecord.Timestamp);

            return timeRecord;
        }

        public async Task<TimeRecord> CheckInAsync(int employeeId, CheckInDto checkInDto)
        {
            // Validar que el empleado no esté ya fichado
            if (await IsEmployeeCheckedInAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado ya tiene un fichaje de entrada activo");
            }

            return await CreateTimeRecordAsync(employeeId, RecordType.CheckIn, checkInDto);
        }

        public async Task<TimeRecord> CheckOutAsync(int employeeId, CheckOutDto checkOutDto)
        {
            // Validar que el empleado esté fichado
            if (!await IsEmployeeCheckedInAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado no tiene un fichaje de entrada activo");
            }

            // Si el empleado está en descanso, terminarlo automáticamente
            if (await IsEmployeeOnBreakAsync(employeeId))
            {
                await EndBreakAsync(employeeId, new CheckOutDto
                {
                    Timestamp = checkOutDto.Timestamp,
                    Notes = "Descanso terminado automáticamente por fichaje de salida",
                    DeviceInfo = checkOutDto.DeviceInfo
                });
            }

            var checkInDto = new CheckInDto
            {
                Timestamp = checkOutDto.Timestamp,
                Notes = checkOutDto.Notes,
                Latitude = checkOutDto.Latitude,
                Longitude = checkOutDto.Longitude,
                Location = checkOutDto.Location,
                DeviceInfo = checkOutDto.DeviceInfo
            };

            return await CreateTimeRecordAsync(employeeId, RecordType.CheckOut, checkInDto);
        }

        public async Task<TimeRecord> StartBreakAsync(int employeeId, CheckInDto breakDto)
        {
            // Validar que el empleado esté fichado
            if (!await IsEmployeeCheckedInAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado debe estar fichado para iniciar un descanso");
            }

            // Validar que no esté ya en descanso
            if (await IsEmployeeOnBreakAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado ya está en descanso");
            }

            return await CreateTimeRecordAsync(employeeId, RecordType.BreakStart, breakDto);
        }

        public async Task<TimeRecord> EndBreakAsync(int employeeId, CheckOutDto breakDto)
        {
            // Validar que el empleado esté en descanso
            if (!await IsEmployeeOnBreakAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado no está en descanso");
            }

            var checkInDto = new CheckInDto
            {
                Timestamp = breakDto.Timestamp,
                Notes = breakDto.Notes,
                Latitude = breakDto.Latitude,
                Longitude = breakDto.Longitude,
                Location = breakDto.Location,
                DeviceInfo = breakDto.DeviceInfo
            };

            return await CreateTimeRecordAsync(employeeId, RecordType.BreakEnd, checkInDto);
        }

        public async Task<TimeRecord?> GetLastOpenRecordAsync(int employeeId)
        {
            var checkInRecord = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Type == RecordType.CheckIn)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync();

            if (checkInRecord == null) return null;

            // Verificar si hay un CheckOut posterior
            var checkOutRecord = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && 
                           tr.Type == RecordType.CheckOut && 
                           tr.Timestamp > checkInRecord.Timestamp)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync();

            return checkOutRecord == null ? checkInRecord : null;
        }

        public async Task<TimeRecord?> GetLastRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TimeRecord>> GetTimeRecordsAsync(int? employeeId = null, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.TimeRecords.Include(tr => tr.Employee).AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(tr => tr.EmployeeId == employeeId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(tr => tr.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(tr => tr.Timestamp <= to.Value);
            }

            return await query
                .OrderByDescending(tr => tr.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeRecord>> GetDailyRecordsAsync(int employeeId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && 
                           tr.Timestamp >= startOfDay && 
                           tr.Timestamp <= endOfDay)
                .OrderBy(tr => tr.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> HasOpenRecordAsync(int employeeId)
        {
            var lastOpenRecord = await GetLastOpenRecordAsync(employeeId);
            return lastOpenRecord != null;
        }

        public async Task<bool> IsEmployeeCheckedInAsync(int employeeId)
        {
            return await HasOpenRecordAsync(employeeId);
        }

        public async Task<bool> IsEmployeeOnBreakAsync(int employeeId)
        {
            // Verificar que esté fichado primero
            if (!await IsEmployeeCheckedInAsync(employeeId))
                return false;

            // Buscar el último BreakStart
            var lastBreakStart = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Type == RecordType.BreakStart)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync();

            if (lastBreakStart == null) return false;

            // Verificar si hay un BreakEnd posterior
            var breakEnd = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && 
                           tr.Type == RecordType.BreakEnd && 
                           tr.Timestamp > lastBreakStart.Timestamp)
                .AnyAsync();

            return !breakEnd;
        }

        public async Task<TimeSpan> CalculateWorkedHoursAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            var totalWorked = TimeSpan.Zero;
            
            TimeRecord? lastCheckIn = null;

            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                switch (record.Type)
                {
                    case RecordType.CheckIn:
                        lastCheckIn = record;
                        break;
                        
                    case RecordType.CheckOut:
                        if (lastCheckIn != null)
                        {
                            var workedTime = record.Timestamp - lastCheckIn.Timestamp;
                            totalWorked = totalWorked.Add(workedTime);
                            lastCheckIn = null;
                        }
                        break;
                }
            }

            // Si hay un CheckIn sin CheckOut, calcular hasta ahora (solo si es hoy)
            if (lastCheckIn != null && date.Date == DateTime.Today)
            {
                var workedTime = DateTime.Now - lastCheckIn.Timestamp;
                totalWorked = totalWorked.Add(workedTime);
            }

            // Restar tiempo de descansos
            var breakTime = await CalculateBreakTimeAsync(employeeId, date);
            totalWorked = totalWorked.Subtract(breakTime);

            return totalWorked > TimeSpan.Zero ? totalWorked : TimeSpan.Zero;
        }

        public async Task<TimeSpan> CalculateBreakTimeAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            var totalBreak = TimeSpan.Zero;
            
            TimeRecord? lastBreakStart = null;

            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                switch (record.Type)
                {
                    case RecordType.BreakStart:
                        lastBreakStart = record;
                        break;
                        
                    case RecordType.BreakEnd:
                        if (lastBreakStart != null)
                        {
                            var breakTime = record.Timestamp - lastBreakStart.Timestamp;
                            totalBreak = totalBreak.Add(breakTime);
                            lastBreakStart = null;
                        }
                        break;
                }
            }

            // Si hay un BreakStart sin BreakEnd, calcular hasta ahora (solo si es hoy)
            if (lastBreakStart != null && date.Date == DateTime.Today)
            {
                var breakTime = DateTime.Now - lastBreakStart.Timestamp;
                totalBreak = totalBreak.Add(breakTime);
            }

            return totalBreak > TimeSpan.Zero ? totalBreak : TimeSpan.Zero;
        }

        public async Task<bool> ValidateCheckInAsync(int employeeId)
        {
            return !await IsEmployeeCheckedInAsync(employeeId);
        }

        public async Task<bool> ValidateCheckOutAsync(int employeeId)
        {
            return await IsEmployeeCheckedInAsync(employeeId);
        }

        public async Task<string> GetEmployeeStatusAsync(int employeeId)
        {
            if (!await IsEmployeeCheckedInAsync(employeeId))
            {
                return "Fuera de oficina";
            }

            if (await IsEmployeeOnBreakAsync(employeeId))
            {
                return "En descanso";
            }

            return "Trabajando";
        }
    }
}