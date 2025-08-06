using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models.Core
{
    [Table("companies")]
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(50)]
        public string? Subdomain { get; set; }

        [StringLength(20)]
        public string? TaxId { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Website { get; set; }

        // Configuración de trabajo
        public TimeSpan? WorkStartTime { get; set; }

        public TimeSpan? WorkEndTime { get; set; }

        public int? ToleranceMinutes { get; set; }

        public int? VacationDaysPerYear { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        // Propiedades calculadas
        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(Code) ? $"{Name} ({Code})" : Name;

        [NotMapped]
        public string FullUrl => !string.IsNullOrEmpty(Subdomain) ? $"https://{Subdomain}.tudominio.com" : "";

        [NotMapped]
        public int EmployeeCount => Employees?.Count ?? 0;

        [NotMapped]
        public int ActiveEmployeeCount => Employees?.Count(e => e.Active) ?? 0;
    }
}