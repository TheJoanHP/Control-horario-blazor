using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    public class UpdateEmployeeDto
    {
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? FirstName { get; set; }
        
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        public string? LastName { get; set; }
        
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Phone { get; set; }
        
        [StringLength(50, ErrorMessage = "El código de empleado no puede exceder 50 caracteres")]
        public string? EmployeeCode { get; set; }
        
        public UserRole? Role { get; set; }
        
        public int? DepartmentId { get; set; }
        
        public DateTime? HiredAt { get; set; }
        
        public DateTime? TerminatedAt { get; set; }
        
        // Horarios personalizados (opcionales)
        public TimeSpan? CustomWorkStartTime { get; set; }
        public TimeSpan? CustomWorkEndTime { get; set; }
        
        public bool? Active { get; set; }
    }
}