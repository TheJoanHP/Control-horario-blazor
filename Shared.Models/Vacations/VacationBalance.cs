using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;

namespace Shared.Models.Vacations
{
    /// <summary>
    /// Saldo de vacaciones de un empleado
    /// </summary>
    [Table("VacationBalances")]
    public class VacationBalance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int Year { get; set; }

        public int TotalDays { get; set; } = 22; // Días por defecto

        public int UsedDays { get; set; } = 0;

        public int PendingDays { get; set; } = 0;

        // AGREGADA la propiedad que falta
        public int CarriedOverDays { get; set; } = 0; // Días arrastrados del año anterior

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public int RemainingDays => TotalDays + CarriedOverDays - UsedDays - PendingDays;

        [NotMapped]
        public int AvailableDays => TotalDays + CarriedOverDays - PendingDays;
    }
}