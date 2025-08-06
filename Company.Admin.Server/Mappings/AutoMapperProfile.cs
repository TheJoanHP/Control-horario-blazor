using AutoMapper;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;
using Shared.Models.DTOs.Employee;
using Shared.Models.DTOs.Department;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.DTOs.Vacations;

namespace Company.Admin.Server.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Mapeo de User
            CreateMap<User, EmployeeDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.Active));

            // Mapeo de Employee
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : src.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.User != null ? src.User.Role : src.Role))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
                .ForMember(dest => dest.YearsOfService, opt => opt.MapFrom(src => 
                    src.HireDate.HasValue ? (DateTime.UtcNow - src.HireDate.Value).Days / 365 : (int?)null));

            CreateMap<Employee, EmployeeListDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => 
                    src.User != null 
                        ? $"{src.User.FirstName} {src.User.LastName}".Trim()
                        : $"{src.FirstName} {src.LastName}".Trim()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : src.Email))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => src.HireDate ?? DateTime.Today));

            CreateMap<CreateEmployeeDto, Employee>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.TimeRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VacationRequests, opt => opt.Ignore())
                .ForMember(dest => dest.VacationBalances, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => src.HireDate ?? DateTime.Today))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true));

            CreateMap<CreateEmployeeDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Shared.Models.Enums.UserRole.Employee))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Mapeo de Department
            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.EmployeeCount, opt => opt.MapFrom(src => src.Employees != null ? src.Employees.Count : 0));

            CreateMap<CreateDepartmentDto, Department>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.Employees, opt => opt.Ignore())
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Mapeo de TimeRecord - CORREGIDO
            CreateMap<TimeRecord, TimeRecordDto>()
                .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.Date.Add(src.Time)))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null && src.Employee.User != null 
                        ? $"{src.Employee.User.FirstName} {src.Employee.User.LastName}".Trim()
                        : src.Employee != null 
                            ? $"{src.Employee.FirstName} {src.Employee.LastName}".Trim()
                            : ""))
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => 
                    src.Employee != null ? src.Employee.EmployeeCode : null));

            // CORREGIDO: CheckInDto no tiene propiedad Type, se debe mapear usando el parámetro RecordType
            CreateMap<CheckInDto, TimeRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore()) // Se debe establecer manualmente en el servicio
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Timestamp.Date))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src => src.Timestamp.TimeOfDay))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<CheckOutDto, TimeRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore()) // Se debe establecer manualmente en el servicio
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Timestamp.Date))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src => src.Timestamp.TimeOfDay))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<CreateTimeRecordDto, TimeRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Timestamp.Date))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src => src.Timestamp.TimeOfDay))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Mapeo de VacationRequest
            CreateMap<VacationRequest, VacationRequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null && src.Employee.User != null 
                        ? $"{src.Employee.User.FirstName} {src.Employee.User.LastName}".Trim()
                        : src.Employee != null 
                            ? $"{src.Employee.FirstName} {src.Employee.LastName}".Trim()
                            : ""))
                .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => src.TotalDays))
                .ForMember(dest => dest.ApprovedById, opt => opt.MapFrom(src => src.ApprovedByUserId))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => 
                    src.ApprovedByUser != null 
                        ? $"{src.ApprovedByUser.FirstName} {src.ApprovedByUser.LastName}".Trim()
                        : null))
                .ForMember(dest => dest.ApprovedAt, opt => opt.MapFrom(src => src.ApprovedAt))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.AdminComments));

            CreateMap<CreateVacationRequestDto, VacationRequest>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Shared.Models.Enums.VacationStatus.Pending))
                .ForMember(dest => dest.DaysRequested, opt => opt.MapFrom(src => (src.EndDate - src.StartDate).Days + 1))
                .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => (src.EndDate - src.StartDate).Days + 1))
                .ForMember(dest => dest.ApprovedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Mapeo de VacationBalance
            CreateMap<VacationBalance, VacationBalanceDto>()
                .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => (decimal)src.TotalDays))
                .ForMember(dest => dest.UsedDays, opt => opt.MapFrom(src => (decimal)src.UsedDays))
                .ForMember(dest => dest.RemainingDays, opt => opt.MapFrom(src => (decimal)src.RemainingDays))
                .ForMember(dest => dest.CarryOverDays, opt => opt.MapFrom(src => 0m)) // Valor por defecto decimal
                .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null && src.Employee.User != null 
                        ? $"{src.Employee.User.FirstName} {src.Employee.User.LastName}".Trim()
                        : src.Employee != null 
                            ? $"{src.Employee.FirstName} {src.Employee.LastName}".Trim()
                            : ""));


        }
    }
}