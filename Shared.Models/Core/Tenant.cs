using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Core
{
    public class Tenant
    {
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty; // Para subdominio
        
        [MaxLength(255)]
        public string? Domain { get; set; } // Dominio personalizado
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public bool Active { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Información de contacto
        [EmailAddress, MaxLength(255)]
        public string ContactEmail { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? ContactPhone { get; set; }
        
        // Configuración
        public string DatabaseName => $"tenant_{Code}";
        
        // Navegación
        public License? License { get; set; }
        public ICollection<SphereAdmin> Admins { get; set; } = new List<SphereAdmin>();
    }
}