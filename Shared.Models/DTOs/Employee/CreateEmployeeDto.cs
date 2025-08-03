using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string Email { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Phone { get; set; }
        
        [StringLength(50, ErrorMessage = "El código de empleado no puede exceder 50 caracteres")]
        public string? EmployeeCode { get; set; }
        
        public UserRole Role { get; set; } = UserRole.Employee;
        
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [StringLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
        public string Password { get; set; } = string.Empty;
        
        public int? DepartmentId { get; set; }
        
        public DateTime? HiredAt { get; set; }
        
        // Horarios personalizados (opcionales)
        public TimeSpan? CustomWorkStartTime { get; set; }
        public TimeSpan? CustomWorkEndTime { get; set; }
        
        public bool Active { get; set; } = true;
    }
}