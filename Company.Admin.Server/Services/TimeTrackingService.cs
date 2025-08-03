using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Company.Admin.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.Enums;

namespace Company.Admin.Server.Services
{
    public interface ITimeTrackingService
    {
        Task<TimeRecordDto> CheckInAsync(int employeeId, CheckInDto checkInDto);
        Task<TimeRecordDto> CheckOutAsync(int employeeId, CheckOutDto checkOutDto);
        Task<TimeRecordDto> StartBreakAsync(int employeeId, string? reason = null);
        Task<TimeRecordDto> EndBreakAsync(int employeeId);
        Task<bool> IsEmployeeCheckedInAsync(int employeeId);
        Task<bool> IsEmployeeOnBreakAsync(int employeeId);
        Task<EmployeeStatusDto> GetEmployeeStatusAsync(int employeeId);
        Task<IEnumerable<TimeRecordDto>> GetEmployeeTimeRecordsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<TimeRecordDto>> GetTimeRecordsAsync(DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null, int? departmentId = null);
        Task<TimeRecordDto?> GetActiveTimeRecordAsync(int employeeId);
        Task<DailyHoursSummaryDto> GetDailyHoursSummaryAsync(int employeeId, DateTime date);
        Task<WeeklyHoursSummaryDto> GetWeeklyHoursSummaryAsync(int employeeId, DateTime weekStart);
        Task<MonthlyHoursSummaryDto> GetMonthlyHoursSummaryAsync(int employeeId, int year, int month);
    }

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

        public async Task<TimeRecordDto> CheckInAsync(int employeeId, CheckInDto checkInDto)
        {
            try
            {
                // Verificar que el empleado no esté ya fichado
                if (await IsEmployeeCheckedInAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado ya está fichado de entrada");
                }

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employeeId,
                    Date = checkInDto.Timestamp.Date,
                    CheckIn = checkInDto.Timestamp,
                    RecordType = RecordType.CheckIn,
                    Location = checkInDto.Location,
                    Notes = checkInDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Check-in registrado para empleado {EmployeeId} a las {Time}", 
                    employeeId, checkInDto.Timestamp);

                return _mapper.Map<TimeRecordDto>(timeRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en check-in para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecordDto> CheckOutAsync(int employeeId, CheckOutDto checkOutDto)
        {
            try
            {
                // Obtener el registro activo de entrada
                var activeRecord = await GetActiveTimeRecordAsync(employeeId);
                if (activeRecord == null)
                {
                    throw new InvalidOperationException("No hay un registro de entrada activo para el empleado");
                }

                var timeRecord = await _context.TimeRecords
                    .FirstOrDefaultAsync(tr => tr.Id == activeRecord.Id);

                if (timeRecord == null)
                {
                    throw new InvalidOperationException("Registro de tiempo no encontrado");
                }

                // Finalizar cualquier pausa activa
                if (await IsEmployeeOnBreakAsync(employeeId))
                {
                    await EndBreakAsync(employeeId);
                }

                timeRecord.CheckOut = checkOutDto.Timestamp;
                timeRecord.UpdatedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(checkOutDto.Notes))
                {
                    timeRecord.Notes = timeRecord.Notes + " | " + checkOutDto.Notes;
                }

                // Calcular horas trabajadas
                timeRecord.TotalHours = CalculateWorkedHours(timeRecord);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Check-out registrado para empleado {EmployeeId} a las {Time}, Total horas: {Hours}", 
                    employeeId, checkOutDto.Timestamp, timeRecord.TotalHours);

                return _mapper.Map<TimeRecordDto>(timeRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en check-out para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecordDto> StartBreakAsync(int employeeId, string? reason = null)
        {
            try
            {
                // Verificar que el empleado esté fichado
                if (!await IsEmployeeCheckedInAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado debe estar fichado para iniciar una pausa");
                }

                // Verificar que no esté ya en pausa
                if (await IsEmployeeOnBreakAsync(employeeId))
                {
                    throw new InvalidOperationException("El empleado ya está en pausa");
                }

                var breakRecord = new TimeRecord
                {
                    EmployeeId = employeeId,
                    Date = DateTime.Today,
                    CheckIn = DateTime.UtcNow,
                    RecordType = RecordType.Break,
                    Notes = reason,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(breakRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pausa iniciada para empleado {EmployeeId} a las {Time}", 
                    employeeId, DateTime.UtcNow);

                return _mapper.Map<TimeRecordDto>(breakRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando pausa para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<TimeRecordDto> EndBreakAsync(int employeeId)
        {
            try
            {
                var activeBreak = await _context.TimeRecords
                    .FirstOrDefaultAsync(tr => tr.EmployeeId == employeeId && 
                                             tr.RecordType == RecordType.Break && 
                                             tr.CheckOut == null &&
                                             tr.Date == DateTime.Today);

                if (activeBreak == null)
                {
                    throw new InvalidOperationException("No hay una pausa activa para el empleado");
                }

                activeBreak.CheckOut = DateTime.UtcNow;
                activeBreak.TotalHours = CalculateBreakTime(activeBreak);
                activeBreak.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pausa finalizada para empleado {EmployeeId} a las {Time}, Duración: {Minutes} minutos", 
                    employeeId, DateTime.UtcNow, activeBreak.TotalHours * 60);

                return _mapper.Map<TimeRecordDto>(activeBreak);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizando pausa para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<bool> IsEmployeeCheckedInAsync(int employeeId)
        {
            try
            {
                return await _context.TimeRecords
                    .AnyAsync(tr => tr.EmployeeId == employeeId && 
                                   tr.RecordType == RecordType.CheckIn && 
                                   tr.CheckOut == null &&
                                   tr.Date == DateTime.Today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando check-in para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<bool> IsEmployeeOnBreakAsync(int employeeId)
        {
            try
            {
                return await _context.TimeRecords
                    .AnyAsync(tr => tr.EmployeeId == employeeId && 
                                   tr.RecordType == RecordType.Break && 
                                   tr.CheckOut == null &&
                                   tr.Date == DateTime.Today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando pausa para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<EmployeeStatusDto> GetEmployeeStatusAsync(int employeeId)
        {
            try
            {
                var isCheckedIn = await IsEmployeeCheckedInAsync(employeeId);
                var isOnBreak = await IsEmployeeOnBreakAsync(employeeId);
                var activeRecord = await GetActiveTimeRecordAsync(employeeId);

                return new EmployeeStatusDto
                {
                    EmployeeId = employeeId,
                    IsCheckedIn = isCheckedIn,
                    IsOnBreak = isOnBreak,
                    CheckInTime = activeRecord?.CheckIn,
                    CurrentBreakStart = isOnBreak ? await GetCurrentBreakStartAsync(employeeId) : null,
                    WorkedHoursToday = await GetWorkedHoursTodayAsync(employeeId),
                    LastUpdate = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<TimeRecordDto>> GetEmployeeTimeRecordsAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(tr => tr.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(tr => tr.Date <= endDate.Value.Date);
                }

                var records = await query
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.CheckIn)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<TimeRecordDto>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registros para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<TimeRecordDto>> GetTimeRecordsAsync(DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null, int? departmentId = null)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(tr => tr.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(tr => tr.Date <= endDate.Value.Date);
                }

                if (employeeId.HasValue)
                {
                    query = query.Where(tr => tr.EmployeeId == employeeId);
                }

                if (departmentId.HasValue)
                {
                    query = query.Where(tr => tr.Employee.DepartmentId == departmentId);
                }

                var records = await query
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.CheckIn)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<TimeRecordDto>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registros de tiempo");
                throw;
            }
        }

        public async Task<TimeRecordDto?> GetActiveTimeRecordAsync(int employeeId)
        {
            try
            {
                var activeRecord = await _context.TimeRecords
                    .FirstOrDefaultAsync(tr => tr.EmployeeId == employeeId && 
                                             tr.RecordType == RecordType.CheckIn && 
                                             tr.CheckOut == null &&
                                             tr.Date == DateTime.Today);

                return activeRecord != null ? _mapper.Map<TimeRecordDto>(activeRecord) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registro activo para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<DailyHoursSummaryDto> GetDailyHoursSummaryAsync(int employeeId, DateTime date)
        {
            try
            {
                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && tr.Date == date.Date)
                    .ToListAsync();

                var workRecords = records.Where(r => r.RecordType == RecordType.CheckIn).ToList();
                var breakRecords = records.Where(r => r.RecordType == RecordType.Break).ToList();

                var totalWorkedHours = workRecords.Sum(r => r.TotalHours ?? 0);
                var totalBreakHours = breakRecords.Sum(r => r.TotalHours ?? 0);

                return new DailyHoursSummaryDto
                {
                    Date = date.Date,
                    EmployeeId = employeeId,
                    TotalWorkedHours = totalWorkedHours,
                    TotalBreakHours = totalBreakHours,
                    CheckInTime = workRecords.FirstOrDefault()?.CheckIn,
                    CheckOutTime = workRecords.FirstOrDefault()?.CheckOut,
                    IsComplete = workRecords.Any() && workRecords.All(r => r.CheckOut.HasValue)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen diario para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<WeeklyHoursSummaryDto> GetWeeklyHoursSummaryAsync(int employeeId, DateTime weekStart)
        {
            try
            {
                var weekEnd = weekStart.AddDays(6);
                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && 
                               tr.Date >= weekStart.Date && 
                               tr.Date <= weekEnd.Date &&
                               tr.RecordType == RecordType.CheckIn)
                    .ToListAsync();

                var totalHours = records.Sum(r => r.TotalHours ?? 0);
                var daysWorked = records.Select(r => r.Date).Distinct().Count();

                return new WeeklyHoursSummaryDto
                {
                    WeekStart = weekStart.Date,
                    WeekEnd = weekEnd.Date,
                    EmployeeId = employeeId,
                    TotalHours = totalHours,
                    DaysWorked = daysWorked,
                    AverageHoursPerDay = daysWorked > 0 ? totalHours / daysWorked : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen semanal para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<MonthlyHoursSummaryDto> GetMonthlyHoursSummaryAsync(int employeeId, int year, int month)
        {
            try
            {
                var monthStart = new DateTime(year, month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && 
                               tr.Date >= monthStart && 
                               tr.Date <= monthEnd &&
                               tr.RecordType == RecordType.CheckIn)
                    .ToListAsync();

                var totalHours = records.Sum(r => r.TotalHours ?? 0);
                var daysWorked = records.Select(r => r.Date).Distinct().Count();

                return new MonthlyHoursSummaryDto
                {
                    Year = year,
                    Month = month,
                    EmployeeId = employeeId,
                    TotalHours = totalHours,
                    DaysWorked = daysWorked,
                    AverageHoursPerDay = daysWorked > 0 ? totalHours / daysWorked : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen mensual para empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        #region Private Methods

        private double CalculateWorkedHours(TimeRecord timeRecord)
        {
            if (timeRecord.CheckIn == null || timeRecord.CheckOut == null)
                return 0;

            var totalMinutes = (timeRecord.CheckOut.Value - timeRecord.CheckIn.Value).TotalMinutes;
            
            // Restar tiempo de pausas del mismo día
            var breakTime = _context.TimeRecords
                .Where(tr => tr.EmployeeId == timeRecord.EmployeeId && 
                           tr.Date == timeRecord.Date &&
                           tr.RecordType == RecordType.Break &&
                           tr.CheckOut != null)
                .Sum(tr => tr.TotalHours ?? 0);

            var workedHours = (totalMinutes / 60) - breakTime;
            return Math.Max(0, workedHours); // No permitir horas negativas
        }

        private double CalculateBreakTime(TimeRecord breakRecord)
        {
            if (breakRecord.CheckIn == null || breakRecord.CheckOut == null)
                return 0;

            var totalMinutes = (breakRecord.CheckOut.Value - breakRecord.CheckIn.Value).TotalMinutes;
            return totalMinutes / 60;
        }

        private async Task<DateTime?> GetCurrentBreakStartAsync(int employeeId)
        {
            var currentBreak = await _context.TimeRecords
                .FirstOrDefaultAsync(tr => tr.EmployeeId == employeeId && 
                                         tr.RecordType == RecordType.Break && 
                                         tr.CheckOut == null &&
                                         tr.Date == DateTime.Today);

            return currentBreak?.CheckIn;
        }

        private async Task<double> GetWorkedHoursTodayAsync(int employeeId)
        {
            var todayRecords = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && 
                           tr.Date == DateTime.Today &&
                           tr.RecordType == RecordType.CheckIn)
                .ToListAsync();

            return todayRecords.Sum(r => r.TotalHours ?? 0);
        }

        #endregion
    }
}