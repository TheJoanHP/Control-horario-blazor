using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models.Core
{
    [Table("system_configs")]
    public class SystemConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? Category { get; set; }

        public bool IsEditable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Propiedades calculadas
        [NotMapped]
        public string DisplayValue => Value.Length > 50 ? $"{Value[..50]}..." : Value;
    }
}