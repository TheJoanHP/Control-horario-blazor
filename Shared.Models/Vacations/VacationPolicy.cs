using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;

namespace Shared.Models.Vacations
{
    /// <summary>
    /// Política de vacaciones de la empresa
    /// </summary>
    [Table("VacationPolicies")]
    public class VacationPolicy
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DefaultDaysPerYear { get; set; } = 22;

        public int MinimumServiceMonths { get; set; } = 0;

        public int MaxConsecutiveDays { get; set; } = 15;

        public int MinAdvanceNoticeDays { get; set; } = 15;

        public bool RequireApproval { get; set; } = true;

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }
    }
}