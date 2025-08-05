namespace Shared.Models.DTOs.Reports
{
    public class HoursReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public List<HoursRecordDto> Records { get; set; } = new();
    }

    public class HoursRecordDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal OvertimeHours { get; set; }
    }
}