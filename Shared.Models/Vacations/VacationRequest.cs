using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;
using Shared.Models.Core;

namespace Shared.Models.Vacations
{
    public class VacationRequest
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public int DaysRequested { get; set; }
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        public VacationStatus Status { get; set; } = VacationStatus.Pending;
        
        [MaxLength(500)]
        public string? AdminNotes { get; set; }
        
        public int? ApprovedBy { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
        public User? ApprovedByUser { get; set; }
    }
}