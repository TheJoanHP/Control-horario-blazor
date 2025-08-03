// Shared.Models/DTOs/Reports/HoursReportDto.cs
namespace Shared.Models.DTOs.Reports
{
    public class HoursReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public decimal RegularHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal TotalHours { get; set; }
        public decimal ExpectedHours { get; set; }
        public decimal Efficiency { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}