using Shared.Models.Vacations;

namespace Employee.App.Server.Services
{
    public interface IEmployeeAppService
    {
        Task<object> GetEmployeeDashboardAsync(int employeeId);
        Task<IEnumerable<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId);
        Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime startDate, DateTime endDate, string? comments);
        Task<object> GetVacationBalanceAsync(int employeeId);
        Task<bool> CanRequestVacationAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<object> GetEmployeeProfileAsync(int employeeId);
        Task<bool> UpdateEmployeeProfileAsync(int employeeId, string firstName, string lastName, string? phone);
    }
}