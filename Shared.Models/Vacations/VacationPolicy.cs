using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Vacations
{
    public class VacationPolicy
    {
        public int Id { get; set; }
        
        public int CompanyId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public int AnnualDays { get; set; } = 22;
        
        public int MaxConsecutiveDays { get; set; } = 15;
        
        public int MinAdvanceNoticeDays { get; set; } = 15;
        
        public bool RequireApproval { get; set; } = true;
        
        public bool CarryOverEnabled { get; set; } = true;
        
        public int MaxCarryOverDays { get; set; } = 5;
        
        public bool Active { get; set; } = true;
        
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
        
        public DateTime? EffectiveTo { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}