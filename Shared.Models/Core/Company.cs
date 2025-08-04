using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Vacations;

namespace Shared.Models.Core
{
    /// <summary>
    /// Representa una empresa en el sistema (tabla en BD del tenant)
    /// </summary>
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? TaxId { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        public bool Active { get; set; } = true;

        public TimeSpan WorkStartTime { get; set; } = new TimeSpan(9, 0, 0); // 09:00

        public TimeSpan WorkEndTime { get; set; } = new TimeSpan(17, 0, 0); // 17:00

        public int ToleranceMinutes { get; set; } = 15;

        public int VacationDaysPerYear { get; set; } = 22;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<VacationPolicy> VacationPolicies { get; set; } = new List<VacationPolicy>();

        // Propiedades calculadas
        [NotMapped]
        public TimeSpan WorkingHours => WorkEndTime - WorkStartTime;

        [NotMapped]
        public int TotalEmployees => Employees?.Count(e => e.Active) ?? 0;

        [NotMapped]
        public int TotalDepartments => Departments?.Count(d => d.Active) ?? 0;
    }
}