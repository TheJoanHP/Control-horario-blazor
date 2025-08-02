using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.Core
{
    public class License
    {
        public int Id { get; set; }
        
        public int TenantId { get; set; }
        
        public LicenseType Type { get; set; } = LicenseType.Basic;
        
        public int MaxEmployees { get; set; } = 10;
        
        public bool HasReports { get; set; } = false;
        
        public bool HasAPI { get; set; } = false;
        
        public bool HasMobileApp { get; set; } = true;
        
        public decimal MonthlyPrice { get; set; } = 0;
        
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndDate { get; set; }
        
        public bool Active { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Tenant Tenant { get; set; } = null!;
    }
}