using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.Enums;
using Shared.Services.Utils;

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
            var employee = await _context.Employees
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                throw new InvalidOperationException("Empleado no encontrado");
            }

            var timeRecord = new TimeRecord
            {
                EmployeeId = employeeId,
                Type = type,
                Timestamp = checkInDto.Timestamp ?? DateTime.UtcNow,
                Location = checkInDto.Location,
                Notes = checkInDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TimeRecords.Add(timeRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registro de tiempo creado: {RecordId} - {EmployeeId} - {Type}", 
                timeRecord.Id, employeeId, type);

            return timeRecord;
        }

        public async Task<TimeRecord> CheckInAsync(int employeeId, CheckInDto checkInDto)
        {
            // Validar que no esté ya registrado
            if (!await ValidateCheckInAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado ya tiene una entrada registrada sin salida");
            }

            return await CreateTimeRecordAsync(employeeId, RecordType.CheckIn, checkInDto);
        }

        public async Task<TimeRecord> CheckOutAsync(int employeeId, CheckOutDto checkOutDto)
        {
            // Validar que tenga una entrada sin salida
            if (!await ValidateCheckOutAsync(employeeId))
            {
                throw new InvalidOperationException("No hay una entrada registrada para hacer checkout");
            }

            var checkInRecord = await GetLastOpenRecordAsync(employeeId);
            if (checkInRecord == null)
            {
                throw new InvalidOperationException("No se encontró el registro de entrada");
            }

            var checkOutRecord = new TimeRecord
            {
                EmployeeId = employeeId,
                Type = RecordType.CheckOut,
                Timestamp = checkOutDto.Timestamp ?? DateTime.UtcNow,
                Location = checkOutDto.Location,
                Notes = checkOutDto.Notes,
                PairedRecordId = checkInRecord.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Actualizar el registro de entrada con el ID del checkout
            checkInRecord.PairedRecordId = checkOutRecord.Id;
            checkInRecord.UpdatedAt = DateTime.UtcNow;

            _context.TimeRecords.Add(checkOutRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Checkout registrado: {RecordId} - {EmployeeId}", 
                checkOutRecord.Id, employeeId);

            return checkOutRecord;
        }

        public async Task<TimeRecord> StartBreakAsync(int employeeId, CheckInDto breakDto)
        {
            // Verificar que esté checked in
            if (!await IsEmployeeCheckedInAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado debe estar registrado para tomar un descanso");
            }

            // Verificar que no esté ya en descanso
            if (await IsEmployeeOnBreakAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado ya está en descanso");
            }

            return await CreateTimeRecordAsync(employeeId, RecordType.BreakStart, breakDto);
        }

        public async Task<TimeRecord> EndBreakAsync(int employeeId, CheckOutDto breakDto)
        {
            // Verificar que esté en descanso
            if (!await IsEmployeeOnBreakAsync(employeeId))
            {
                throw new InvalidOperationException("El empleado no está en descanso");
            }

            var breakStartRecord = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Type == RecordType.BreakStart)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync(tr => tr.PairedRecordId == null);

            if (breakStartRecord == null)
            {
                throw new InvalidOperationException("No se encontró el inicio del descanso");
            }

            var breakEndRecord = new TimeRecord
            {
                EmployeeId = employeeId,
                Type = RecordType.BreakEnd,
                Timestamp = breakDto.Timestamp ?? DateTime.UtcNow,
                Location = breakDto.Location,
                Notes = breakDto.Notes,
                PairedRecordId = breakStartRecord.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Actualizar el registro de inicio de descanso
            breakStartRecord.PairedRecordId = breakEndRecord.Id;
            breakStartRecord.UpdatedAt = DateTime.UtcNow;

            _context.TimeRecords.Add(breakEndRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fin de descanso registrado: {RecordId} - {EmployeeId}", 
                breakEndRecord.Id, employeeId);

            return breakEndRecord;
        }

        public async Task<TimeRecord?> GetLastOpenRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Type == RecordType.CheckIn)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync(tr => tr.PairedRecordId == null);
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
                .ThenInclude(e => e!.Department)
                .AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(tr => tr.EmployeeId == employeeId);
            }

            if (from.HasValue)
            {
                query = query.Where(tr => tr.Timestamp >= from);
            }

            if (to.HasValue)
            {
                query = query.Where(tr => tr.Timestamp <= to);
            }

            return await query
                .OrderByDescending(tr => tr.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeRecord>> GetDailyRecordsAsync(int employeeId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId &&
                            tr.Timestamp >= startOfDay &&
                            tr.Timestamp < endOfDay)
                .OrderBy(tr => tr.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> HasOpenRecordAsync(int employeeId)
        {
            return await _context.TimeRecords
                .AnyAsync(tr => tr.EmployeeId == employeeId && 
                              tr.Type == RecordType.CheckIn && 
                              tr.PairedRecordId == null);
        }

        public async Task<bool> IsEmployeeCheckedInAsync(int employeeId)
        {
            return await HasOpenRecordAsync(employeeId);
        }

        public async Task<bool> IsEmployeeOnBreakAsync(int employeeId)
        {
            return await _context.TimeRecords
                .AnyAsync(tr => tr.EmployeeId == employeeId && 
                              tr.Type == RecordType.BreakStart && 
                              tr.PairedRecordId == null);
        }

        public async Task<TimeSpan> CalculateWorkedHoursAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            var checkInRecords = records.Where(r => r.Type == RecordType.CheckIn).ToList();
            var checkOutRecords = records.Where(r => r.Type == RecordType.CheckOut).ToList();

            TimeSpan totalWorked = TimeSpan.Zero;

            foreach (var checkIn in checkInRecords)
            {
                var matchingCheckOut = checkOutRecords.FirstOrDefault(co => co.PairedRecordId == checkIn.Id);
                if (matchingCheckOut != null)
                {
                    totalWorked += matchingCheckOut.Timestamp - checkIn.Timestamp;
                }
            }

            // Restar tiempo de descansos
            var breakTime = await CalculateBreakTimeAsync(employeeId, date);
            totalWorked -= breakTime;

            return totalWorked > TimeSpan.Zero ? totalWorked : TimeSpan.Zero;
        }

        public async Task<TimeSpan> CalculateBreakTimeAsync(int employeeId, DateTime date)
        {
            var records = await GetDailyRecordsAsync(employeeId, date);
            var breakStartRecords = records.Where(r => r.Type == RecordType.BreakStart).ToList();
            var breakEndRecords = records.Where(r => r.Type == RecordType.BreakEnd).ToList();

            TimeSpan totalBreakTime = TimeSpan.Zero;

            foreach (var breakStart in breakStartRecords)
            {
                var matchingBreakEnd = breakEndRecords.FirstOrDefault(be => be.PairedRecordId == breakStart.Id);
                if (matchingBreakEnd != null)
                {
                    totalBreakTime += matchingBreakEnd.Timestamp - breakStart.Timestamp;
                }
            }

            return totalBreakTime;
        }

        public async Task<bool> ValidateCheckInAsync(int employeeId)
        {
            // No debe tener un check-in abierto
            return !await HasOpenRecordAsync(employeeId);
        }

        public async Task<bool> ValidateCheckOutAsync(int employeeId)
        {
            // Debe tener un check-in abierto
            return await HasOpenRecordAsync(employeeId);
        }

        public async Task<string> GetEmployeeStatusAsync(int employeeId)
        {
            if (await IsEmployeeOnBreakAsync(employeeId))
            {
                return "En descanso";
            }

            if (await IsEmployeeCheckedInAsync(employeeId))
            {
                return "Trabajando";
            }

            return "Fuera";
        }
    }
}