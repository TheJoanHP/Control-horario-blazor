using System.ComponentModel.DataAnnotations;
using Shared.Models.Core;
using Shared.Models.Enums;

namespace Shared.Models.Vacations
{
    public class VacationRequest
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public int DaysRequested { get; set; }
        
        [MaxLength(1000)]
        public string? Comments { get; set; }
        
        public VacationStatus Status { get; set; } = VacationStatus.Pending;
        
        [MaxLength(1000)]
        public string? ResponseComments { get; set; }
        
        public int? ReviewedById { get; set; }
        
        public DateTime? ReviewedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
        public Employee? ReviewedBy { get; set; }
    }
}