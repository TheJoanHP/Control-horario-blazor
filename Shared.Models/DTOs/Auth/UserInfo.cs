using Shared.Models.Enums;

namespace Shared.Models.DTOs.Auth
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool Active { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }

        // Propiedades calculadas
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email;
    }
}