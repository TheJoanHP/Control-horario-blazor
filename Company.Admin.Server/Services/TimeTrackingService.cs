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
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null)
                    throw new ArgumentException("Empleado no encontrado");

                var currentTime = DateTime.Now;
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employeeId,
                    Type = type,
                    Date = currentTime.Date,
                    Time = currentTime.TimeOfDay,
                    Timestamp = currentTime,
                    Notes = checkInDto.Notes,
                    Location = checkInDto.Location,
                    Latitude = checkInDto.Latitude,
                    Longitude = checkInDto.Longitude,
                    IsManualEntry = checkInDto.IsManualEntry,
                    CreatedAt = currentTime
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

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
                // Validar que no tenga un check-in abierto
                if (!await ValidateCheckInAsync(employeeId))
                {
                    throw new InvalidOperationException("Ya existe un fichaje de entrada sin salida");
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
                // Validar que tenga un check-in abierto
                if (!await ValidateCheckOutAsync(employeeId))
                {
                    throw new InvalidOperationException("No hay un fichaje de entrada activo");
                }

                var checkInDto = new CheckInDto
                {
                    EmployeeId = employeeId,
                    Notes = checkOutDto.Notes,
                    Location = checkOutDto.Location,
                    Latitude = checkOutDto.Latitude,
                    Longitude = checkOutDto.Longitude,
                    IsManualEntry = checkOutDto.IsManualEntry,
                    CreatedByUserId = checkOutDto.CreatedByUserId
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
                // Validar que esté fichado y no esté en descanso
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
                // Validar que esté en descanso
                if (!await IsEmployeeOnBreakAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado no está en descanso");
                }

                var checkInDto = new CheckInDto
                {
                    EmployeeId = employeeId,
                    Notes = breakDto.Notes,
                    Location = breakDto.Location,
                    Latitude = breakDto.Latitude,
                    Longitude = breakDto.Longitude,
                    IsManualEntry = breakDto.IsManualEntry,
                    CreatedByUserId = breakDto.CreatedByUserId
                };

                return await CreateTimeRecordAsync(employeeId, RecordType.BreakEnd, checkInDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminando descanso para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecord?> GetLastOpenRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId)
                .Where(tr => tr.Type == RecordType.CheckIn || tr.Type == RecordType.BreakStart)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync();
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
            var query = _context.TimeRecords
                .Include(tr => tr.Employee)
                .ThenInclude(e => e.User)
                .AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(tr => tr.EmployeeId == employeeId);

            if (from.HasValue)
                query = query.Where(tr => tr.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(tr => tr.Date <= to.Value.Date);

            return await query
                .OrderByDescending(tr => tr.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeRecord>> GetDailyRecordsAsync(int employeeId, DateTime date)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == date.Date)
                .OrderBy(tr => tr.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> HasOpenRecordAsync(int employeeId)
        {
            var lastRecord = await GetLastRecordAsync(employeeId);
            
            if (lastRecord == null) return false;

            // Si el último registro es CheckIn o BreakStart, hay un registro abierto
            return lastRecord.Type == RecordType.CheckIn || lastRecord.Type == RecordType.BreakStart;
        }

        public async Task<bool> IsEmployeeCheckedInAsync(int employeeId)
        {
            var todayRecords = await GetDailyRecordsAsync(employeeId, DateTime.Today);
            
            var checkIns = todayRecords.Count(r => r.Type == RecordType.CheckIn);
            var checkOuts = todayRecords.Count(r => r.Type == RecordType.CheckOut);
            
            return checkIns > checkOuts;
        }

        public async Task<bool> IsEmployeeOnBreakAsync(int employeeId)
        {
            var todayRecords = await GetDailyRecordsAsync(employeeId, DateTime.Today);
            
            var breakStarts = todayRecords.Count(r => r.Type == RecordType.BreakStart);
            var breakEnds = todayRecords.Count(r => r.Type == RecordType.BreakEnd);
            
            return breakStarts > breakEnds;
        }

        public async Task<TimeSpan> CalculateWorkedHoursAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            
            var workTime = TimeSpan.Zero;
            var breakTime = TimeSpan.Zero;
            
            DateTime? checkInTime = null;
            DateTime? breakStartTime = null;
            
            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                switch (record.Type)
                {
                    case RecordType.CheckIn:
                        checkInTime = record.Timestamp;
                        break;
                        
                    case RecordType.CheckOut:
                        if (checkInTime.HasValue)
                        {
                            workTime = workTime.Add(record.Timestamp - checkInTime.Value);
                            checkInTime = null;
                        }
                        break;
                        
                    case RecordType.BreakStart:
                        breakStartTime = record.Timestamp;
                        break;
                        
                    case RecordType.BreakEnd:
                        if (breakStartTime.HasValue)
                        {
                            breakTime = breakTime.Add(record.Timestamp - breakStartTime.Value);
                            breakStartTime = null;
                        }
                        break;
                }
            }
            
            // Si aún está trabajando, calcular hasta ahora
            if (checkInTime.HasValue && date.Date == DateTime.Today)
            {
                workTime = workTime.Add(DateTime.Now - checkInTime.Value);
            }
            
            // Restar tiempo de descanso del tiempo trabajado
            return workTime.Subtract(breakTime);
        }

        public async Task<TimeSpan> CalculateBreakTimeAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            
            var breakTime = TimeSpan.Zero;
            DateTime? breakStartTime = null;
            
            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                switch (record.Type)
                {
                    case RecordType.BreakStart:
                        breakStartTime = record.Timestamp;
                        break;
                        
                    case RecordType.BreakEnd:
                        if (breakStartTime.HasValue)
                        {
                            breakTime = breakTime.Add(record.Timestamp - breakStartTime.Value);
                            breakStartTime = null;
                        }
                        break;
                }
            }
            
            // Si aún está en descanso, calcular hasta ahora
            if (breakStartTime.HasValue && date.Date == DateTime.Today)
            {
                breakTime = breakTime.Add(DateTime.Now - breakStartTime.Value);
            }
            
            return breakTime;
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
            var isCheckedIn = await IsEmployeeCheckedInAsync(employeeId);
            var isOnBreak = await IsEmployeeOnBreakAsync(employeeId);
            
            if (!isCheckedIn)
                return "Fuera";
            
            if (isOnBreak)
                return "En descanso";
                
            return "Trabajando";
        }
    }
}