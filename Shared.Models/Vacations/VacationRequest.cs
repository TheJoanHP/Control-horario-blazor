using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;
using Shared.Models.Core;

namespace Shared.Models.Vacations
{
    [Table("vacation_requests")]
    public class VacationRequest
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int DaysRequested { get; set; }

        public int TotalDays { get; set; } // Alias para DaysRequested

        public VacationStatus Status { get; set; } = VacationStatus.Pending;

        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(1000)]
        public string? AdminComments { get; set; }

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedByUser { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public string StatusDisplay => Status switch
        {
            VacationStatus.Pending => "Pendiente",
            VacationStatus.Approved => "Aprobada",
            VacationStatus.Rejected => "Rechazada",
            VacationStatus.Cancelled => "Cancelada",
            _ => Status.ToString()
        };

        [NotMapped]
        public bool CanEdit => Status == VacationStatus.Pending;

        [NotMapped]
        public bool IsActive => Status == VacationStatus.Approved && EndDate >= DateTime.Today;

        [NotMapped]
        public int CalculatedDays
        {
            get
            {
                var days = (EndDate - StartDate).Days + 1;
                // Solo contar días laborables (opcional)
                var businessDays = 0;
                for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                        businessDays++;
                }
                return businessDays;
            }
        }

        // Sincronizar propiedades alias
        public void SyncTotalDays()
        {
            TotalDays = DaysRequested;
        }
    }
}