using Shared.Models.Core;

namespace Company.Admin.Server.Services
{
    public interface IDepartmentService
    {
        Task<Department> CreateDepartmentAsync(string name, string? description = null);
        Task<Department> UpdateDepartmentAsync(int id, string name, string? description = null);
        Task<Department?> GetDepartmentByIdAsync(int id);
        Task<IEnumerable<Department>> GetDepartmentsAsync(bool? active = null);
        Task<IEnumerable<Department>> GetActiveDepartmentsAsync();
        Task<bool> DeleteDepartmentAsync(int id);
        Task<bool> ActivateDepartmentAsync(int id);
        Task<bool> DeactivateDepartmentAsync(int id);
        Task<int> GetEmployeeCountAsync(int departmentId);
        Task<bool> CanDeleteDepartmentAsync(int departmentId);
        Task<bool> IsDepartmentNameUniqueAsync(string name, int? excludeId = null);
    }
}