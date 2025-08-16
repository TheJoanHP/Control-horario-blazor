using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    /// <summary>
    /// Licencia de un tenant (empresa/cliente)
    /// </summary>
    public class License
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public LicenseType LicenseType { get; set; }

        [Required]
        [Range(1, 9999)]
        public int MaxEmployees { get; set; }

        [Required]
        [Range(0, 999999.99)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MonthlyPrice { get; set; }

        public bool Active { get; set; } = true;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Características de la licencia
        public bool HasReports { get; set; } = false;
        public bool HasAdvancedReports { get; set; } = false;
        public bool HasMobileApp { get; set; } = false;
        public bool HasAPI { get; set; } = false;
        public bool HasGeolocation { get; set; } = false;

        // Auditoría
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // *** NAVEGACIÓN ***
        public virtual Tenant? Tenant { get; set; }

        // *** PROPIEDADES CALCULADAS ***
        /// <summary>
        /// Verifica si la licencia está expirada
        /// </summary>
        [NotMapped]
        public bool IsExpired => EndDate < DateTime.UtcNow;

        /// <summary>
        /// Verifica si la licencia expira pronto (en 7 días)
        /// </summary>
        [NotMapped]
        public bool IsExpiringSoon => EndDate <= DateTime.UtcNow.AddDays(7) && !IsExpired;

        /// <summary>
        /// Días restantes de la licencia
        /// </summary>
        [NotMapped]
        public int DaysRemaining => IsExpired ? 0 : (int)(EndDate - DateTime.UtcNow).TotalDays;

        /// <summary>
        /// Estado de la licencia como texto
        /// </summary>
        [NotMapped]
        public string StatusText => IsExpired ? "Expirada" : IsExpiringSoon ? "Por expirar" : "Activa";
    }
}