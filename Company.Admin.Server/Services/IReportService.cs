using Shared.Models.DTOs.Reports;

namespace Company.Admin.Server.Services
{
    public interface IReportService
    {
        Task<IEnumerable<AttendanceReportDto>> GenerateAttendanceReportAsync(AttendanceReportRequest request);
        Task<IEnumerable<HoursReportDto>> GenerateHoursReportAsync(HoursReportRequest request);
        Task<SummaryReportDto> GenerateSummaryReportAsync(DateTime fromDate, DateTime toDate);
        Task<byte[]> ExportAttendanceReportToExcelAsync(AttendanceReportRequest request);
        Task<byte[]> ExportHoursReportToExcelAsync(HoursReportRequest request);
        Task<byte[]> ExportSummaryReportToExcelAsync(DateTime fromDate, DateTime toDate);
        Task<string> ExportAttendanceReportToCsvAsync(AttendanceReportRequest request);
        Task<string> ExportHoursReportToCsvAsync(HoursReportRequest request);
        Task<Dictionary<string, object>> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, object>> GetEmployeeStatsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}