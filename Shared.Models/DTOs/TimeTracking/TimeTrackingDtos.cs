using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    // DTOs para Check In/Out
    public class CheckInDto
    {
        public int EmployeeId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    public class CheckOutDto
    {
        public int EmployeeId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    public class BreakStartDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    public class BreakEndDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    // DTOs para TimeRecord
    public class TimeRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public RecordType Type { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime DateTime { get; set; }
        public DateTime Timestamp => DateTime; // Alias para compatibilidad
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Propiedades adicionales para reportes
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public double TotalHours { get; set; }
        public RecordType RecordType => Type; // Alias
        
        public string TypeDisplay => Type switch
        {
            RecordType.CheckIn => "Entrada",
            RecordType.CheckOut => "Salida", 
            RecordType.BreakStart => "Inicio Descanso",
            RecordType.BreakEnd => "Fin Descanso",
            _ => Type.ToString()
        };
    }

    public class CreateTimeRecordDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public RecordType Type { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime Date => Timestamp.Date;
        public TimeSpan Time => Timestamp.TimeOfDay;

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
    }

    public class UpdateTimeRecordDto
    {
        public DateTime? Timestamp { get; set; }
        public DateTime? Date => Timestamp?.Date;
        public TimeSpan? Time => Timestamp?.TimeOfDay;
        public RecordType? Type { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}