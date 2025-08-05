using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;
using Shared.Models.DTOs.TimeTracking;
using Company.Admin.Server.Data;

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

        public async Task<TimeRecord> CheckInAsync(CheckInDto checkInDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(checkInDto.EmployeeId);
                if (employee == null)
                    throw new ArgumentException("Empleado no encontrado");

                var today = DateTime.Today;
                var currentTime = DateTime.Now;

                // Verificar si ya tiene un check-in hoy sin check-out
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == checkInDto.EmployeeId && tr.Date == today)
                    .OrderByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord != null && lastRecord.Type == RecordType.CheckIn)
                {
                    throw new InvalidOperationException("Ya existe un fichaje de entrada sin salida para hoy");
                }

                var timeRecord = new TimeRecord
                {
                    EmployeeId = checkInDto.EmployeeId,
                    Type = RecordType.CheckIn,
                    Date = currentTime.Date,
                    Time = currentTime.TimeOfDay,
                    Notes = checkInDto.Notes,
                    Location = checkInDto.Location,
                    Latitude = checkInDto.Latitude,
                    Longitude = checkInDto.Longitude,
                    IsManualEntry = checkInDto.IsManualEntry,
                    CreatedByUserId = checkInDto.CreatedByUserId,
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return timeRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar check-in para empleado {EmployeeId}", checkInDto.EmployeeId);
                throw;
            }
        }

        public async Task<TimeRecord> CheckOutAsync(CheckOutDto checkOutDto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(checkOutDto.EmployeeId);
                if (employee == null)
                    throw new ArgumentException("Empleado no encontrado");

                var today = DateTime.Today;
                var currentTime = DateTime.Now;

                // Verificar que tenga un check-in activo hoy
                var lastCheckIn = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == checkOutDto.EmployeeId && 
                                tr.Date == today && 
                                tr.Type == RecordType.CheckIn)
                    .OrderByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastCheckIn == null)
                {
                    throw new InvalidOperationException("No se encontró un fichaje de entrada para hoy");
                }

                // Verificar que no tenga ya un check-out posterior
                var hasCheckOut = await _context.TimeRecords
                    .AnyAsync(tr => tr.EmployeeId == checkOutDto.EmployeeId && 
                                   tr.Date == today && 
                                   tr.Type == RecordType.CheckOut &&
                                   tr.Time > lastCheckIn.Time);

                if (hasCheckOut)
                {
                    throw new InvalidOperationException("Ya existe un fichaje de salida posterior al último fichaje de entrada");
                }

                var timeRecord = new TimeRecord
                {
                    EmployeeId = checkOutDto.EmployeeId,
                    Type = RecordType.CheckOut,
                    Date = currentTime.Date,
                    Time = currentTime.TimeOfDay,
                    Notes = checkOutDto.Notes,
                    Location = checkOutDto.Location,
                    Latitude = checkOutDto.Latitude,
                    Longitude = checkOutDto.Longitude,
                    IsManualEntry = checkOutDto.IsManualEntry,
                    CreatedByUserId = checkOutDto.CreatedByUserId,
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return timeRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar check-out para empleado {EmployeeId}", checkOutDto.EmployeeId);
                throw;
            }
        }

        public async Task<IEnumerable<TimeRecord>> GetEmployeeRecordsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId);

            if (startDate.HasValue)
                query = query.Where(tr => tr.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(tr => tr.Date <= endDate.Value.Date);

            return await query
                .OrderByDescending(tr => tr.Date)
                .ThenByDescending(tr => tr.Time)
                .ToListAsync();
        }

        public async Task<TimeRecord?> GetLastRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId)
                .OrderByDescending(tr => tr.Date)
                .ThenByDescending(tr => tr.Time)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsEmployeeCheckedInAsync(int employeeId)
        {
            var today = DateTime.Today;
            
            var lastRecord = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                .OrderByDescending(tr => tr.Time)
                .FirstOrDefaultAsync();

            return lastRecord?.Type == RecordType.CheckIn;
        }

        public async Task<Dictionary<string, object>> GetEmployeeDailyStatsAsync(int employeeId, DateTime date)
        {
            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == date.Date)
                .OrderBy(tr => tr.Time)
                .ToListAsync();

            var stats = new Dictionary<string, object>
            {
                ["Date"] = date.Date,
                ["TotalRecords"] = records.Count,
                ["CheckIns"] = records.Count(r => r.Type == RecordType.CheckIn),
                ["CheckOuts"] = records.Count(r => r.Type == RecordType.CheckOut),
                ["BreakStarts"] = records.Count(r => r.Type == RecordType.BreakStart),
                ["BreakEnds"] = records.Count(r => r.Type == RecordType.BreakEnd),
                ["FirstCheckIn"] = records.FirstOrDefault(r => r.Type == RecordType.CheckIn)?.Time,
                ["LastCheckOut"] = records.LastOrDefault(r => r.Type == RecordType.CheckOut)?.Time,
                ["TotalWorkTime"] = CalculateWorkTime(records),
                ["TotalBreakTime"] = CalculateBreakTime(records)
            };

            return stats;
        }

        private static TimeSpan CalculateWorkTime(List<TimeRecord> records)
        {
            var workTime = TimeSpan.Zero;
            TimeSpan? checkInTime = null;

            foreach (var record in records.OrderBy(r => r.Time))
            {
                switch (record.Type)
                {
                    case RecordType.CheckIn:
                        checkInTime = record.Time;
                        break;
                    case RecordType.CheckOut:
                        if (checkInTime.HasValue)
                        {
                            workTime = workTime.Add(record.Time - checkInTime.Value);
                            checkInTime = null;
                        }
                        break;
                }
            }

            return workTime;
        }

        private static TimeSpan CalculateBreakTime(List<TimeRecord> records)
        {
            var breakTime = TimeSpan.Zero;
            TimeSpan? breakStartTime = null;

            foreach (var record in records.OrderBy(r => r.Time))
            {
                switch (record.Type)
                {
                    case RecordType.BreakStart:
                        breakStartTime = record.Time;
                        break;
                    case RecordType.BreakEnd:
                        if (breakStartTime.HasValue)
                        {
                            breakTime = breakTime.Add(record.Time - breakStartTime.Value);
                            breakStartTime = null;
                        }
                        break;
                }
            }

            return breakTime;
        }
    }
}