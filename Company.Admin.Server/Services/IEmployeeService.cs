using Shared.Models.Core;
using Shared.Models.DTOs.Employee;

namespace Company.Admin.Server.Services
{
    public interface IEmployeeService
    {
        Task<Employee> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto);
        Task<Employee> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<Employee?> GetEmployeeByEmailAsync(string email);
        Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, bool? active = null);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> ActivateEmployeeAsync(int id);
        Task<bool> DeactivateEmployeeAsync(int id);
        Task<bool> ChangePasswordAsync(int id, string newPassword);
        Task<bool> ValidateEmployeeCredentialsAsync(string email, string password);
        Task<Employee?> AuthenticateEmployeeAsync(string email, string password);
        Task<int> GetTotalEmployeesAsync();
        Task<int> GetActiveEmployeesAsync();
        Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);
        Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, int? excludeId = null);
        Task<string> GenerateUniqueEmployeeCodeAsync();
    }
}