using Shared.Models.TimeTracking;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.Enums;

namespace Company.Admin.Server.Services
{
    public interface ITimeTrackingService
    {
        Task<TimeRecord> CreateTimeRecordAsync(int employeeId, RecordType type, CheckInDto checkInDto);
        Task<TimeRecord> CheckInAsync(int employeeId, CheckInDto checkInDto);
        Task<TimeRecord> CheckOutAsync(int employeeId, CheckOutDto checkOutDto);
        Task<TimeRecord> StartBreakAsync(int employeeId, CheckInDto breakDto);
        Task<TimeRecord> EndBreakAsync(int employeeId, CheckOutDto breakDto);
        Task<TimeRecord?> GetLastOpenRecordAsync(int employeeId);
        Task<TimeRecord?> GetLastRecordAsync(int employeeId);
        Task<IEnumerable<TimeRecord>> GetTimeRecordsAsync(int? employeeId = null, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<TimeRecord>> GetDailyRecordsAsync(int employeeId, DateTime date);
        Task<bool> HasOpenRecordAsync(int employeeId);
        Task<bool> IsEmployeeCheckedInAsync(int employeeId);
        Task<bool> IsEmployeeOnBreakAsync(int employeeId);
        Task<TimeSpan> CalculateWorkedHoursAsync(int employeeId, DateTime date);
        Task<TimeSpan> CalculateBreakTimeAsync(int employeeId, DateTime date);
        Task<bool> ValidateCheckInAsync(int employeeId);
        Task<bool> ValidateCheckOutAsync(int employeeId);
        Task<string> GetEmployeeStatusAsync(int employeeId);
    }
}