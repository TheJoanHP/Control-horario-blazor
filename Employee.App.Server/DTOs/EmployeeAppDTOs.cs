using System.ComponentModel.DataAnnotations;

namespace Employee.App.Server.DTOs
{
    public class PinLoginRequest
    {
        [Required]
        public string Pin { get; set; } = string.Empty;
    }

    public class EmployeeChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class TimeClockRequest
    {
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class VacationRequestDto
    {
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string? Comments { get; set; }
        
        public string? Reason { get; set; }
    }

    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? Phone { get; set; }
    }
}