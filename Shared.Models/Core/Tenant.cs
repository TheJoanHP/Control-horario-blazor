using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    [Table("tenants")]
    public class Tenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Subdomain { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string DatabaseName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        public LicenseType LicenseType { get; set; } = LicenseType.Trial;

        public int MaxEmployees { get; set; } = 10;

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public virtual License? License { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public string DisplayName => $"{Name} ({Code})";

        [NotMapped]
        public string FullUrl => $"https://{Subdomain}.tudominio.com";
    }
}