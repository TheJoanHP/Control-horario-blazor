using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Department
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Active { get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }
    }

    public class UpdateDepartmentDto
    {
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? Name { get; set; }
        
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? Description { get; set; }
        
        public bool? Active { get; set; }
    }

    public class DepartmentStatsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public double TotalHoursThisMonth { get; set; }
        public double AverageHoursPerEmployee { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}