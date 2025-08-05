using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models.Core
{
    [Table("tenants")]
    public class Tenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Subdomain { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public bool Active { get; set; } = true;

        public int MaxEmployees { get; set; } = 50;

        [Required]
        [StringLength(255)]
        public string DatabaseName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ConnectionString { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    }
}