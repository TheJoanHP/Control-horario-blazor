// Ruta: Shared.Models/Core/SystemConfig.cs
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Core
{
    /// <summary>
    /// Configuración del sistema
    /// </summary>
    public class SystemConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Value { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = "General";

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string DataType { get; set; } = "string";

        public bool IsEditable { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        // Auditoría
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }
}