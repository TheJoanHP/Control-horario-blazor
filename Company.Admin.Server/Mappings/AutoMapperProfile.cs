using AutoMapper;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.TimeTracking;
using Shared.Models.DTOs.Department;
using Shared.Models.DTOs.Vacations;
using Shared.Models.Vacations;

namespace Company.Admin.Server.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Mapeos de Employee
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : string.Empty))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : string.Empty))
                .ForMember(dest => dest.YearsOfService, opt => opt.MapFrom(src => 
                    src.HireDate.HasValue ? (DateTime.UtcNow - src.HireDate.Value).Days / 365 : (int?)null));

            CreateMap<CreateEmployeeDto, Employee>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true));

            CreateMap<UpdateEmployeeDto, Employee>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mapeos de Department
            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.EmployeeCount, opt => opt.MapFrom(src => src.Employees != null ? src.Employees.Count : 0));

            CreateMap<CreateDepartmentDto, Department>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true));

            // Mapeos de TimeRecord
            CreateMap<TimeRecord, TimeRecordDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null && src.Employee.User != null ? src.Employee.User.FullName : string.Empty))
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => 
                    src.Employee != null ? src.Employee.EmployeeCode : string.Empty))
                .ForMember(dest => dest.DateTime, opt => opt.MapFrom(src => src.Date.Add(src.Time)));

            CreateMap<CheckInDto, TimeRecord>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Timestamp.Date))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src => src.Timestamp.TimeOfDay))
                .ForMember(dest => dest.CheckIn, opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(dest => dest.CheckOut, opt => opt.Ignore())
                .ForMember(dest => dest.TotalHours, opt => opt.Ignore())
                .ForMember(dest => dest.RecordType, opt => opt.MapFrom(src => src.Type));

            // Mapeo de Company (corregido - usar el tipo completo)
            CreateMap<Shared.Models.Core.Company, Shared.Models.DTOs.Auth.CompanyInfo>()
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.Active));

            // Mapeos de Vacaciones
            CreateMap<VacationRequest, VacationRequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? src.Employee.FullName : string.Empty))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => 
                    src.ApprovedByUser != null ? src.ApprovedByUser.FullName : string.Empty));

            CreateMap<CreateVacationRequestDto, VacationRequest>()
                .ForMember(dest => dest.DaysRequested, opt => opt.MapFrom(src => 
                    (src.EndDate - src.StartDate).Days + 1))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<VacationBalance, VacationBalanceDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? src.Employee.FullName : string.Empty));
        }
    }
}