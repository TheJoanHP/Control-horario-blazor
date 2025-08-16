using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    /// <summary>
    /// Representa una empresa/cliente (tenant) en el sistema
    /// </summary>
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
        [EmailAddress]
        [StringLength(255)]
        public string ContactEmail { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? TaxId { get; set; }

        [Url]
        [StringLength(255)]
        public string? Website { get; set; }

        [Url]
        [StringLength(500)]
        public string? LogoUrl { get; set; }

        public bool Active { get; set; } = true;

        [Required]
        public LicenseType LicenseType { get; set; } = LicenseType.Trial;

        [Range(1, 9999)]
        public int MaxEmployees { get; set; } = 10;

        [Range(0, 999999.99)]
        public decimal MonthlyPrice { get; set; } = 0.00m;

        [StringLength(3)]
        public string Currency { get; set; } = "EUR";

        public DateTime? TrialStartedAt { get; set; }
        public DateTime? TrialEndedAt { get; set; }
        public DateTime? LastPaymentAt { get; set; }
        
        // Auditoría
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // *** RELACIONES DE NAVEGACIÓN ***
        // Singular: Para compatibilidad con el controlador actual
        public virtual License? License { get; set; }
        
        // Plural: Para futuras funcionalidades (historial de licencias)
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();

        // *** PROPIEDADES CALCULADAS ***
        [NotMapped]
        public string DisplayName => $"{Name} ({Code})";

        [NotMapped]
        public string FullUrl => $"https://{Subdomain}.tudominio.com";

        /// <summary>
        /// Obtiene la licencia activa actual
        /// </summary>
        [NotMapped]
        public License? CurrentLicense => License ?? Licenses?.FirstOrDefault(l => l.Active && !l.IsExpired);

        /// <summary>
        /// Verifica si el tenant tiene una licencia válida
        /// </summary>
        [NotMapped]
        public bool HasValidLicense => CurrentLicense != null;

        /// <summary>
        /// Verifica si el tenant está en período de prueba
        /// </summary>
        [NotMapped]
        public bool IsInTrial => LicenseType == LicenseType.Trial;

        /// <summary>
        /// Obtiene el nombre completo del tenant
        /// </summary>
        [NotMapped]
        public string FullName => $"{Name} ({Code})";
    }
}