namespace Shared.Models.DTOs.Reports
{
    public class AttendanceReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRecords { get; set; }
        public List<AttendanceRecordDto> Records { get; set; } = new();
    }

    public class AttendanceRecordDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public decimal HoursWorked { get; set; }
    }
}