using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Company.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.DTOs.Department;

namespace Company.Admin.Server.Services
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetDepartmentsAsync(int companyId, bool? active = null);
        Task<DepartmentDto?> GetDepartmentByIdAsync(int id);
        Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDto, int companyId);
        Task<DepartmentDto?> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDto);
        Task<bool> DeleteDepartmentAsync(int id);
        Task<bool> ActivateDepartmentAsync(int id);
        Task<bool> DeactivateDepartmentAsync(int id);
        Task<int> GetEmployeeCountByDepartmentAsync(int departmentId);
        Task<IEnumerable<DepartmentStatsDto>> GetDepartmentStatsAsync(int companyId);
    }

    public class DepartmentService : IDepartmentService
    {
        private readonly CompanyDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            CompanyDbContext context,
            IMapper mapper,
            ILogger<DepartmentService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<DepartmentDto>> GetDepartmentsAsync(int companyId, bool? active = null)
        {
            try
            {
                var query = _context.Departments
                    .Where(d => d.CompanyId == companyId)
                    .AsQueryable();

                if (active.HasValue)
                {
                    query = query.Where(d => d.Active == active);
                }

                var departments = await query
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo departamentos para empresa {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
        {
            try
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == id);

                return department != null ? _mapper.Map<DepartmentDto>(department) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDto, int companyId)
        {
            try
            {
                // Validar que no exista un departamento con el mismo nombre en la empresa
                var existingDepartment = await _context.Departments
                    .AnyAsync(d => d.CompanyId == companyId && d.Name == createDto.Name);

                if (existingDepartment)
                {
                    throw new InvalidOperationException($"Ya existe un departamento con el nombre '{createDto.Name}'");
                }

                var department = _mapper.Map<Department>(createDto);
                department.CompanyId = companyId;
                department.Active = true;
                department.CreatedAt = DateTime.UtcNow;
                department.UpdatedAt = DateTime.UtcNow;

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento creado: {DepartmentName} para empresa {CompanyId}", 
                    department.Name, companyId);

                return _mapper.Map<DepartmentDto>(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando departamento {DepartmentName}", createDto.Name);
                throw;
            }
        }

        public async Task<DepartmentDto?> UpdateDepartmentAsync(int id, UpdateDepartmentDto updateDto)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return null;

                // Validar que no exista otro departamento con el mismo nombre
                if (!string.IsNullOrWhiteSpace(updateDto.Name) && updateDto.Name != department.Name)
                {
                    var existingDepartment = await _context.Departments
                        .AnyAsync(d => d.CompanyId == department.CompanyId && 
                                      d.Name == updateDto.Name && 
                                      d.Id != id);

                    if (existingDepartment)
                    {
                        throw new InvalidOperationException($"Ya existe un departamento con el nombre '{updateDto.Name}'");
                    }
                }

                _mapper.Map(updateDto, department);
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento actualizado: {DepartmentId} - {DepartmentName}", 
                    id, department.Name);

                return _mapper.Map<DepartmentDto>(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return false;

                // Verificar si tiene empleados asignados
                var hasEmployees = await _context.Employees
                    .AnyAsync(e => e.DepartmentId == id);

                if (hasEmployees)
                {
                    throw new InvalidOperationException("No se puede eliminar un departamento que tiene empleados asignados. Use la desactivación en su lugar.");
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento eliminado: {DepartmentId} - {DepartmentName}", 
                    id, department.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return false;

                department.Active = true;
                department.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento activado: {DepartmentId} - {DepartmentName}", 
                    id, department.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return false;

                department.Active = false;
                department.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento desactivado: {DepartmentId} - {DepartmentName}", 
                    id, department.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<int> GetEmployeeCountByDepartmentAsync(int departmentId)
        {
            try
            {
                return await _context.Employees
                    .CountAsync(e => e.DepartmentId == departmentId && e.Active);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo conteo de empleados para departamento {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<IEnumerable<DepartmentStatsDto>> GetDepartmentStatsAsync(int companyId)
        {
            try
            {
                var stats = await _context.Departments
                    .Where(d => d.CompanyId == companyId)
                    .Select(d => new DepartmentStatsDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Active = d.Active,
                        TotalEmployees = d.Employees.Count,
                        ActiveEmployees = d.Employees.Count(e => e.Active),
                        CreatedAt = d.CreatedAt
                    })
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de departamentos para empresa {CompanyId}", companyId);
                throw;
            }
        }
    }
}