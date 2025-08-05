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

        public int TenantId { get; set; }

        public LicenseType Type { get; set; } = LicenseType.Basic;

        public int MaxUsers { get; set; } = 10;

        public bool Active { get; set; } = true;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
    }
}