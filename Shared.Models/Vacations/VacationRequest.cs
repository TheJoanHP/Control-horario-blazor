using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;
using Shared.Models.Enums;

namespace Shared.Models.Vacations
{
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
        [Column(TypeName = "decimal(5,2)")]
        public decimal DaysRequested { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public VacationStatus Status { get; set; } = VacationStatus.Pending;

        [StringLength(500)]
        public string? Comments { get; set; }

        public int? ReviewedBy { get; set; }

        [StringLength(500)]
        public string? ResponseComments { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual Employee? ReviewedByEmployee { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsPending => Status == VacationStatus.Pending;

        [NotMapped]
        public bool IsApproved => Status == VacationStatus.Approved;

        [NotMapped]
        public bool IsRejected => Status == VacationStatus.Rejected;

        [NotMapped]
        public bool IsCancelled => Status == VacationStatus.Cancelled;

        [NotMapped]
        public int TotalDays => (int)Math.Ceiling((EndDate - StartDate).TotalDays) + 1;
    }
}