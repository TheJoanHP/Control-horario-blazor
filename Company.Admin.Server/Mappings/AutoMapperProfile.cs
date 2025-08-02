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
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.WorkStartTime, opt => opt.Ignore())
                .ForMember(dest => dest.WorkEndTime, opt => opt.Ignore());

            // UpdateEmployeeDto -> Employee (para aplicar cambios)
            CreateMap<UpdateEmployeeDto, Employee>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ForMember(dest => dest.Department, opt => opt.Ignore())
                .ForMember(dest => dest.TimeRecords, opt => opt.Ignore())
                .ForMember(dest => dest.VacationRequests, opt => opt.Ignore())
                .ForMember(dest => dest.FullName, opt => opt.Ignore())
                .ForMember(dest => dest.WorkStartTime, opt => opt.Ignore())
                .ForMember(dest => dest.WorkEndTime, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }

        private void CreateTimeTrackingMappings()
        {
            // TimeRecord -> TimeRecordDto
            CreateMap<TimeRecord, TimeRecordDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.FullName : ""))
                .ForMember(dest => dest.TypeDisplay, opt => opt.MapFrom(src => GetRecordTypeDisplay(src.Type)));

            // CheckInDto -> TimeRecord (parcial, se completa en el servicio)
            CreateMap<CheckInDto, TimeRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp ?? DateTime.UtcNow))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

            // CheckOutDto -> TimeRecord (parcial, se completa en el servicio)
            CreateMap<CheckOutDto, TimeRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EmployeeId, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp ?? DateTime.UtcNow))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());
        }

        private void CreateVacationMappings()
        {
            // VacationRequest -> VacationRequestDto (cuando se cree)
            CreateMap<VacationRequest, object>()
                .ConvertUsing(src => new
                {
                    Id = src.Id,
                    EmployeeId = src.EmployeeId,
                    EmployeeName = src.Employee != null ? src.Employee.FullName : "",
                    StartDate = src.StartDate,
                    EndDate = src.EndDate,
                    DaysRequested = src.DaysRequested,
                    Comments = src.Comments,
                    Status = src.Status.ToString(),
                    ResponseComments = src.ResponseComments,
                    ReviewedById = src.ReviewedById,
                    ReviewedByName = src.ReviewedBy != null ? src.ReviewedBy.FullName : null,
                    ReviewedAt = src.ReviewedAt,
                    CreatedAt = src.CreatedAt,
                    UpdatedAt = src.UpdatedAt
                });

            // VacationBalance -> VacationBalanceDto (cuando se cree)
            CreateMap<VacationBalance, object>()
                .ConvertUsing(src => new
                {
                    Id = src.Id,
                    EmployeeId = src.EmployeeId,
                    EmployeeName = src.Employee != null ? src.Employee.FullName : "",
                    Year = src.Year,
                    TotalDays = src.TotalDays,
                    UsedDays = src.UsedDays,
                    PendingDays = src.PendingDays,
                    CarriedOverDays = src.CarriedOverDays,
                    AvailableDays = src.AvailableDays,
                    RemainingDays = src.RemainingDays
                });
        }

        private void CreateReportMappings()
        {
            // Mapeo para reportes de asistencia
            CreateMap<object, AttendanceReportDto>(); // Se configurará dinámicamente

            // Mapeo para reportes de horas
            CreateMap<object, HoursReportDto>(); // Se configurará dinámicamente

            // Employee + TimeRecord -> AttendanceReportDto (mapeo complejo)
            CreateMap<Employee, AttendanceReportDto>()
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => src.EmployeeCode))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.Date, opt => opt.Ignore())
                .ForMember(dest => dest.CheckInTime, opt => opt.Ignore())
                .ForMember(dest => dest.CheckOutTime, opt => opt.Ignore())
                .ForMember(dest => dest.WorkedHours, opt => opt.Ignore())
                .ForMember(dest => dest.BreakTime, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore())
                .ForMember(dest => dest.IsLate, opt => opt.Ignore())
                .ForMember(dest => dest.IsAbsent, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore());

            // Employee -> HoursReportDto
            CreateMap<Employee, HoursReportDto>()
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.EmployeeCode, opt => opt.MapFrom(src => src.EmployeeCode))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.FromDate, opt => opt.Ignore())
                .ForMember(dest => dest.ToDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWorkedHours, opt => opt.Ignore())
                .ForMember(dest => dest.ExpectedHours, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore())
                .ForMember(dest => dest.BreakTime, opt => opt.Ignore())
                .ForMember(dest => dest.WorkingDays, opt => opt.Ignore())
                .ForMember(dest => dest.AbsentDays, opt => opt.Ignore())
                .ForMember(dest => dest.LateDays, opt => opt.Ignore())
                .ForMember(dest => dest.AttendancePercentage, opt => opt.Ignore());
        }

        // Método auxiliar para mostrar tipos de registro en formato legible
        private static string GetRecordTypeDisplay(RecordType type)
        {
            return type switch
            {
                RecordType.CheckIn => "Entrada",
                RecordType.CheckOut => "Salida",
                RecordType.BreakStart => "Inicio de Descanso",
                RecordType.BreakEnd => "Fin de Descanso",
                _ => type.ToString()
            };
        }
    }
}