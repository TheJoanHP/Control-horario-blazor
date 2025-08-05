using Shared.Models.DTOs.Employee;

namespace Company.Admin.Server.Services
{
    public interface IEmployeeService
    {
        // CRUD Operations
        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto);
        Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateDto);
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeDto?> GetEmployeeByEmailAsync(string email);
        Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null);
        Task<bool> DeleteEmployeeAsync(int id);

        // Status Management
        Task<bool> ActivateEmployeeAsync(int id);
        Task<bool> DeactivateEmployeeAsync(int id);

        // Password Management
        Task<bool> ChangePasswordAsync(int id, string newPassword);

        // Authentication & Validation
        Task<bool> ValidateEmployeeCredentialsAsync(string email, string password);
        Task<EmployeeDto?> AuthenticateEmployeeAsync(string email, string password);

        // Statistics
        Task<int> GetTotalEmployeesAsync();
        Task<int> GetActiveEmployeesAsync();

        // Validation Helpers
        Task<bool> IsEmailUniqueAsync(string email, int? excludeEmployeeId = null);
        Task<bool> IsEmployeeCodeUniqueAsync(string code, int? excludeEmployeeId = null);
        Task<string> GenerateUniqueEmployeeCodeAsync();
    }
}