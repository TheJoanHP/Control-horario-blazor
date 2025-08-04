using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    /// <summary>
    /// DTO para actualizar empleados
    /// </summary>
    public class UpdateEmployeeDto
    {
        public int? DepartmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Position { get; set; }

        public UserRole Role { get; set; }

        public decimal? Salary { get; set; }

        public bool Active { get; set; } = true;
    }
}