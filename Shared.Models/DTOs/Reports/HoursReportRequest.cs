using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Reports
{
    public class HoursReportRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? EmployeeId { get; set; }

        public int? DepartmentId { get; set; }

        public bool IncludeOvertime { get; set; } = true;

        public bool IncludeBreaks { get; set; } = true;

        public string? ExportFormat { get; set; } // "excel", "csv", "pdf"

        public string? GroupBy { get; set; } // "employee", "department", "date"
    }
}