using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    public class License
    {
        public int Id { get; set; }
        
        public int TenantId { get; set; }
        
        public LicenseType Type { get; set; }
        
        public int MaxEmployees { get; set; }
        
        public decimal MonthlyPrice { get; set; }
        
        public bool Active { get; set; } = true;
        
        public DateTime StartsAt { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Tenant Tenant { get; set; } = null!;
    }
}