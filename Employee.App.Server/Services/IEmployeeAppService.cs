using Shared.Models.Vacations;

namespace Employee.App.Server.Services
{
    /// <summary>
    /// Servicio para funcionalidades específicas de la aplicación de empleados
    /// </summary>
    public interface IEmployeeAppService
    {
        // Dashboard
        Task<object> GetEmployeeDashboardAsync(int employeeId);
        
        // Profile Management
        Task<object> GetEmployeeProfileAsync(int employeeId);
        Task<bool> UpdateEmployeeProfileAsync(int employeeId, string firstName, string lastName, string? phone);
        
        // Vacation Management
        Task<IEnumerable<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId);
        Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime startDate, DateTime endDate, string? comments);
        Task<object> GetVacationBalanceAsync(int employeeId);
        Task<bool> CanRequestVacationAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<List<object>> GetRecentVacationRequestsAsync(int employeeId);
        
        // Time Tracking
        Task<object> GetTodayTimeRecordsAsync(int employeeId);
        
        // Alias method for compatibility
        Task<VacationRequest> RequestVacationAsync(int employeeId, DateTime startDate, DateTime endDate, string? comments);
    }
}