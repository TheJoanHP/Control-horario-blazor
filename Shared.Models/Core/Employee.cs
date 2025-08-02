using System.ComponentModel.DataAnnotations;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;

namespace Shared.Models.Core
{
    public class Employee
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        public int CompanyId { get; set; }
        
        [MaxLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;
        
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        
        public bool Active { get; set; } = true;
        
        public string? Pin { get; set; } // PIN para fichaje rápido
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;
        public ICollection<TimeRecord> TimeRecords { get; set; } = new List<TimeRecord>();
        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}