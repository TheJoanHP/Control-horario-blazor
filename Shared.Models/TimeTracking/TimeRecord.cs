using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    [Table("time_records")]
    public class TimeRecord
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public RecordType Type { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan Time { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsManualEntry { get; set; } = false;

        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public DateTime DateTime => Date.Date + Time;

        [NotMapped]
        public DateTime Timestamp 
        { 
            get => Date.Date + Time;
            set 
            { 
                Date = value.Date;
                Time = value.TimeOfDay;
            }
        }

        [NotMapped]
        public string TypeDisplay => Type switch
        {
            RecordType.CheckIn => "Entrada",
            RecordType.CheckOut => "Salida",
            RecordType.BreakStart => "Inicio Descanso",
            RecordType.BreakEnd => "Fin Descanso",
            _ => Type.ToString()
        };

        // Propiedades adicionales para compatibilidad
        [NotMapped]
        public DateTime? CheckIn { get; set; }

        [NotMapped]
        public DateTime? CheckOut { get; set; }

        [NotMapped]
        public double TotalHours { get; set; }

        [NotMapped]
        public RecordType RecordType 
        { 
            get => Type;
            set => Type = value;
        }
    }
}