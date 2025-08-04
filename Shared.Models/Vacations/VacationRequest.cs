using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;
using Shared.Models.Enums;

namespace Shared.Models.Vacations
{
    /// <summary>
    /// Solicitud de vacaciones de un empleado
    /// </summary>
    [Table("VacationRequests")]
    public class VacationRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int DaysRequested { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public VacationStatus Status { get; set; } = VacationStatus.Pending;

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual Employee? ApprovedByEmployee { get; set; }
    }
}