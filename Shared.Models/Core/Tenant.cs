using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    public class Tenant
    {
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required, MaxLength(50)]
        public string Subdomain { get; set; } = string.Empty;
        
        [Required, MaxLength(200)]
        public string DatabaseName { get; set; } = string.Empty;
        
        [EmailAddress, MaxLength(255)]
        public string ContactEmail { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        public bool Active { get; set; } = true;
        
        public LicenseType LicenseType { get; set; } = LicenseType.Trial;
        
        public int MaxEmployees { get; set; } = 10;
        
        public DateTime LicenseExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public License? License { get; set; }
    }
}