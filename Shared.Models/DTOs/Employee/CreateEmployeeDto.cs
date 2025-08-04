using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    /// <summary>
    /// DTO para crear empleados
    /// </summary>
    public class CreateEmployeeDto
    {
        [Required]
        public int CompanyId { get; set; }

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

        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Position { get; set; }

        public UserRole Role { get; set; } = UserRole.Employee;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public DateTime? HireDate { get; set; }

        public decimal? Salary { get; set; }
    }
}