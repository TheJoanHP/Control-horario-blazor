using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Reports
{
    public class AttendanceReportRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? EmployeeId { get; set; }

        public int? DepartmentId { get; set; }

        public bool IncludeInactive { get; set; } = false;

        public string? ExportFormat { get; set; } // "excel", "csv", "pdf"
    }
}