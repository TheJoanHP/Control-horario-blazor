using System.ComponentModel.DataAnnotations;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    public class Overtime
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public DateTime Date { get; set; }
        
        public TimeSpan Duration { get; set; }
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        public bool Approved { get; set; } = false;
        
        public int? ApprovedById { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
        public Employee? ApprovedBy { get; set; }
    }
}