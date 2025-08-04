using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    /// <summary>
    /// Representa una licencia de un tenant
    /// </summary>
    [Table("Licenses")]
    public class License
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        public LicenseType LicenseType { get; set; } = LicenseType.Trial;

        public int MaxEmployees { get; set; } = 5;

        public bool HasReports { get; set; } = false;

        public bool HasAPI { get; set; } = false;

        public bool HasMobileApp { get; set; } = true;

        public decimal MonthlyPrice { get; set; } = 0;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > EndDate;

        [NotMapped]
        public int DaysUntilExpiration => (EndDate - DateTime.UtcNow).Days;
    }
}