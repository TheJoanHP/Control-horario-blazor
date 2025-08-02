using Shared.Models.Core;

namespace Shared.Models.Vacations
{
    public class VacationBalance
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public int Year { get; set; }
        
        public int TotalDays { get; set; }
        
        public int UsedDays { get; set; }
        
        public int PendingDays { get; set; }
        
        public int CarriedOverDays { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Propiedades calculadas
        public int AvailableDays => TotalDays - UsedDays - PendingDays;
        public int RemainingDays => TotalDays - UsedDays;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
    }
}