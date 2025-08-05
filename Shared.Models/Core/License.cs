using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    [Table("licenses")]
    public class License
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public LicenseType LicenseType { get; set; } = LicenseType.Trial;

        public int MaxEmployees { get; set; } = 10;

        public bool HasReports { get; set; } = false;

        public bool HasAPI { get; set; } = false;

        public bool HasMobileApp { get; set; } = true;

        public bool HasGeolocation { get; set; } = false;

        public bool HasAdvancedReports { get; set; } = false;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyPrice { get; set; } = 0m;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);

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
        public int DaysRemaining => Math.Max(0, (EndDate - DateTime.UtcNow).Days);

        [NotMapped]
        public string StatusText => IsExpired ? "Expirada" : Active ? "Activa" : "Inactiva";
    }
}