using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Core
{
    public class Company
    {
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? TaxId { get; set; }
        
        [MaxLength(255)]
        public string? Address { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [EmailAddress, MaxLength(255)]
        public string? Email { get; set; }
        
        public bool Active { get; set; } = true;
        
        // Configuración de horarios
        public TimeSpan WorkStartTime { get; set; } = new TimeSpan(9, 0, 0); // 09:00
        public TimeSpan WorkEndTime { get; set; } = new TimeSpan(17, 0, 0);   // 17:00
        public int ToleranceMinutes { get; set; } = 15; // Tolerancia llegadas tarde
        
        // Configuración de vacaciones
        public int VacationDaysPerYear { get; set; } = 22;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Department> Departments { get; set; } = new List<Department>();
    }
}