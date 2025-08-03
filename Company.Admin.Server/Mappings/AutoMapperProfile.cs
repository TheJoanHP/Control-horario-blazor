using AutoMapper;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;
using Shared.Models.DTOs.Employee;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.DTOs.Reports;
using Shared.Models.Enums;

namespace Company.Admin.Server.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateEmployeeMappings();
            CreateTimeTrackingMappings();
            CreateVacationMappings();
            CreateReportMappings();
        }

        private void CreateEmployeeMappings()
        {
            // Employee -> EmployeeDto
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : ""))
                .ForMember(dest => dest.WorkStartTime, opt => opt.MapFrom(src => src.WorkStartTime))
                .ForMember(dest => dest.WorkEndTime, opt => opt.MapFrom(src => src.WorkEndTime))
                .ForMember(dest => dest.TotalTimeRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VacationDaysUsed, opt => opt.Ignore())
                .ForMember(dest => dest.VacationDaysAvailable, opt => opt.Ignore());

            // CreateEmployeeDto -> Employee
            CreateMap<CreateEmployeeDto, Employee>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Se establece en el servicio
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore()) // Se establece en el servicio
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore())
                .ForMember(dest => dest.TimeRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VacationRequests, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role ?? UserRole.Employee));

            // UpdateEmployeeDto -> Employee
            CreateMap<UpdateEmployeeDto, Employee>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Se maneja por separado
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore())
                .ForMember(dest => dest.TimeRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VacationRequests, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }

        private void CreateTimeTrackingMappings()
        {
            // TimeRecord -> TimeRecordDto
            CreateMap<TimeRecord, TimeRecordDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : ""))
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => 
                    src.Employee != null ? src.Employee.EmployeeCode : ""));

            // CreateTimeRecordDto -> TimeRecord
            CreateMap<CreateTimeRecordDto, TimeRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IpAddress, opt => opt.Ignore()) // Se establece en el controlador
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

            // WorkSchedule mappings
            CreateMap<WorkSchedule, WorkScheduleDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : ""));

            // Break mappings
            CreateMap<Break, BreakDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : ""))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // Overtime mappings
            CreateMap<Overtime, OvertimeDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : ""))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => 
                    src.ApprovedBy != null ? $"{src.ApprovedBy.FirstName} {src.ApprovedBy.LastName}" : null));
        }

        private void CreateVacationMappings()
        {
            // VacationRequest -> VacationRequestDto
            CreateMap<VacationRequest, VacationRequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : ""))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => 
                    src.ApprovedBy != null ? $"{src.ApprovedBy.FirstName} {src.ApprovedBy.LastName}" : null))
                .ForMember(dest => dest.TotalDays, opt => opt.MapFrom(src => src.TotalDays));

            // CreateVacationRequestDto -> VacationRequest
            CreateMap<CreateVacationRequestDto, VacationRequest>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore()) // Se establece en el servicio
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => VacationStatus.Pending))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore());

            // VacationPolicy mappings
            CreateMap<VacationPolicy, VacationPolicyDto>();

            // VacationBalance mappings
            CreateMap<VacationBalance, VacationBalanceDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => 
                    src.Employee != null ? $"{src.Employee.FirstName} {src.Employee.LastName}" : ""));
        }

        private void CreateReportMappings()
        {
            // Mappings para reportes (se pueden expandir según necesidades)
            CreateMap<Employee, AttendanceReportDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => 
                    src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.TotalHours, opt => opt.Ignore())
                .ForMember(dest => dest.WorkingDays, opt => opt.Ignore())
                .ForMember(dest => dest.AbsentDays, opt => opt.Ignore())
                .ForMember(dest => dest.LateDays, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore());

            CreateMap<Employee, HoursReportDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => 
                    src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.RegularHours, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore())
                .ForMember(dest => dest.TotalHours, opt => opt.Ignore())
                .ForMember(dest => dest.ExpectedHours, opt => opt.Ignore())
                .ForMember(dest => dest.Efficiency, opt => opt.Ignore());
        }
    }
}