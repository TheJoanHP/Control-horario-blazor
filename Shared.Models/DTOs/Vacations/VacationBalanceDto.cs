// Shared.Models/DTOs/Vacations/VacationBalanceDto.cs
namespace Shared.Models.DTOs.Vacations
{
    public class VacationBalanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal RemainingDays { get; set; }
        public decimal CarryOverDays { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}