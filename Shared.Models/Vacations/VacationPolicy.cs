using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;

namespace Shared.Models.Vacations
{
    [Table("vacation_policies")]
    public class VacationPolicy
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DaysPerYear { get; set; } = 22;

        public int AnnualDays { get; set; } = 22; // Alias para DaysPerYear

        public int MaxConsecutiveDays { get; set; } = 15;

        public int MaxCarryOver { get; set; } = 5;

        public int MaxCarryOverDays { get; set; } = 5; // Alias para MaxCarryOver

        public bool RequireApproval { get; set; } = true;

        public int MinAdvanceNoticeDays { get; set; } = 15;

        public bool CarryOverEnabled { get; set; } = true;

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        // Propiedades calculadas
        [NotMapped]
        public string DisplayName => $"{Name} ({DaysPerYear} días/año)";

        [NotMapped]
        public bool IsDefault => Name.Contains("General") || Name.Contains("Default");
    }
}