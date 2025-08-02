using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    public class UpdateEmployeeDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El apellido es requerido")]
        [MaxLength(50, ErrorMessage = "El apellido no puede exceder 50 caracteres")]
        public string LastName { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [MaxLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Phone { get; set; }
        
        [MaxLength(50, ErrorMessage = "El código de empleado no puede exceder 50 caracteres")]
        public string? EmployeeCode { get; set; }
        
        public UserRole? Role { get; set; }
        
        public int? DepartmentId { get; set; }
        
        public DateTime? HiredAt { get; set; }
        
        // Horarios personalizados (opcionales)
        public TimeSpan? CustomWorkStartTime { get; set; }
        public TimeSpan? CustomWorkEndTime { get; set; }
        
        public bool? Active { get; set; }
    }
}