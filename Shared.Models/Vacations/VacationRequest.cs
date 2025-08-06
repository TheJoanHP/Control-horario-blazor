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

        // AGREGADAS las propiedades que faltan según la BD
        [StringLength(1000)]
        public string? Comments { get; set; } // Para comentarios del empleado

        [StringLength(1000)]
        public string? AdminComments { get; set; } // Para comentarios del admin

        [StringLength(1000)]
        public string? ResponseComments { get; set; } // Alias para AdminComments

        public int? ApprovedByUserId { get; set; }

        public int? ReviewedById { get; set; } // Alias para ApprovedByUserId

        public DateTime? ApprovedAt { get; set; }

        public DateTime? ReviewedAt { get; set; } // Alias para ApprovedAt

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedByUser { get; set; }

        [ForeignKey("ReviewedById")]
        public virtual Employee? ReviewedBy { get; set; } // Para compatibilidad con Employee

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
        public void SyncProperties()
        {
            TotalDays = DaysRequested;
            ResponseComments = AdminComments;
            ReviewedById = ApprovedByUserId;
            ReviewedAt = ApprovedAt;
        }
    }
}