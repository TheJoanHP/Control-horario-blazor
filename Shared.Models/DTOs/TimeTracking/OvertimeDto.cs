// Shared.Models/DTOs/TimeTracking/OvertimeDto.cs
namespace Shared.Models.DTOs.TimeTracking
{
    public class OvertimeDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Reason { get; set; }
        public bool Approved { get; set; }
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}