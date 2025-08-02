using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Core
{
    public class User
    {
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Role { get; set; } = "EMPLOYEE";
        
        public bool Active { get; set; } = true;
        
        public DateTime? LastLogin { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación (opcional para empleados)
        public Employee? Employee { get; set; }
    }
}