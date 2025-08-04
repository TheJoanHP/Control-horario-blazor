using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.TimeTracking
{
    /// <summary>
    /// DTO para registrar salida
    /// </summary>
    public class CheckOutDto
    {
        public DateTime DateTime { get; set; } = DateTime.Now;
        
        [StringLength(255)]
        public string? Location { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }

        // Propiedades de compatibilidad
        public DateTime Date 
        { 
            get => DateTime.Date; 
            set => DateTime = value.Date + DateTime.TimeOfDay;
        }
        
        public TimeSpan Time 
        { 
            get => DateTime.TimeOfDay; 
            set => DateTime = DateTime.Date + value;
        }

        public DateTime Timestamp
        {
            get => DateTime;
            set => DateTime = value;
        }
    }
}