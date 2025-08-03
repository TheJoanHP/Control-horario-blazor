// Shared.Models/DTOs/Vacations/CreateVacationRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Vacations
{
    public class CreateVacationRequestDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
