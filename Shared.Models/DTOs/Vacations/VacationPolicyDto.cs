// Shared.Models/DTOs/Vacations/VacationPolicyDto.cs
namespace Shared.Models.DTOs.Vacations
{
    public class VacationPolicyDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DaysPerYear { get; set; }
        public int MaxCarryOver { get; set; }
        public bool RequireApproval { get; set; }
        public int MinAdvanceNoticeDays { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}