// ===== Shared/Models/DTOs/Vacations/VacationDtos.cs =====
using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Vacations
{
    public class VacationRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DaysRequested { get; set; }
        public string Reason { get; set; } = string.Empty;
        public VacationStatus Status { get; set; }
        public string? Comments { get; set; }
        public int? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewComments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalDays { get; set; }
        public bool IsPending { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public bool IsCancelled { get; set; }
    }

    public class CreateVacationRequestDto
    {
        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "La fecha de fin es requerida")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "El motivo es requerido")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder 500 caracteres")]
        public string? Comments { get; set; }
    }

    public class UpdateVacationRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string? Reason { get; set; }
        
        [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder 500 caracteres")]
        public string? Comments { get; set; }
    }

    public class ReviewVacationRequestDto
    {
        [Required(ErrorMessage = "El estado es requerido")]
        public VacationStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder 500 caracteres")]
        public string? ReviewComments { get; set; }
    }

    public class VacationBalanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public decimal TotalDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal PendingDays { get; set; }
        public decimal? CarryOverDays { get; set; }
        public decimal AvailableDays { get; set; }
        public decimal RemainingDays { get; set; }
    }

    public class VacationPolicyDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal AnnualDays { get; set; }
        public decimal? MaxConsecutiveDays { get; set; }
        public decimal? MinRequestDays { get; set; }
        public int? MinAdvanceNoticeDays { get; set; }
        public bool RequiresApproval { get; set; }
        public bool AutoApprove { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateVacationPolicyDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Los días anuales son requeridos")]
        [Range(0, 365, ErrorMessage = "Los días anuales deben estar entre 0 y 365")]
        public decimal AnnualDays { get; set; }

        [Range(0, 365, ErrorMessage = "Los días consecutivos máximos deben estar entre 0 y 365")]
        public decimal? MaxConsecutiveDays { get; set; }

        [Range(0, 365, ErrorMessage = "Los días mínimos de solicitud deben estar entre 0 y 365")]
        public decimal? MinRequestDays { get; set; }

        [Range(0, 365, ErrorMessage = "Los días de aviso mínimo deben estar entre 0 y 365")]
        public int? MinAdvanceNoticeDays { get; set; }

        public bool RequiresApproval { get; set; } = true;
        public bool AutoApprove { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
