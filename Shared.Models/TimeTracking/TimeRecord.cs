using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;
using Shared.Models.Enums;

namespace Shared.Models.TimeTracking
{
    /// <summary>
    /// Registro de tiempo de un empleado
    /// </summary>
    [Table("TimeRecords")]
    public class TimeRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public RecordType RecordType { get; set; }

        [Required]
        public DateTime RecordDateTime { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Propiedades calculadas para compatibilidad
        [NotMapped]
        public DateTime Timestamp
        {
            get => RecordDateTime;
            set => RecordDateTime = value;
        }

        [NotMapped]
        public RecordType Type
        {
            get => RecordType;
            set => RecordType = value;
        }

        [NotMapped]
        public TimeSpan Time => RecordDateTime.TimeOfDay;

        [NotMapped]
        public DateTime Date 
        { 
            get => RecordDateTime.Date; 
            set => RecordDateTime = value.Date + RecordDateTime.TimeOfDay;
        }

        [NotMapped]
        public string? DeviceInfo { get; set; }

        [NotMapped]
        public string? IpAddress { get; set; }

        [NotMapped]
        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public DateTime? CheckIn { get; set; }

        [NotMapped]
        public DateTime? CheckOut { get; set; }

        [NotMapped]
        public TimeSpan? TotalHours { get; set; }
    }
}