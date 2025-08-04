using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Company.Admin.Server.Data;
using Shared.Models.Core;

namespace Company.Admin.Server.Services
{
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

        public async Task<Department> CreateDepartmentAsync(string name, string? description = null)
        {
            try
            {
                // Verificar que el nombre no exista
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Name == name);
                
                if (existingDepartment != null)
                {
                    throw new InvalidOperationException("Ya existe un departamento con este nombre");
                }

                var department = new Department
                {
                    Name = name,
                    Description = description,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento creado: {DepartmentId} - {Name}", department.Id, department.Name);
                return department;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando departamento");
                throw;
            }
        }

        public async Task<Department> UpdateDepartmentAsync(int id, string name, string? description = null)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    throw new InvalidOperationException("Departamento no encontrado");
                }

                // Validar nombre único (excluyendo el departamento actual)
                var existingName = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Name == name && d.Id != id);
                
                if (existingName != null)
                {
                    throw new InvalidOperationException("Ya existe un departamento con este nombre");
                }

                department.Name = name;
                department.Description = description;
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento actualizado: {DepartmentId}", department.Id);
                return department;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            return await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsAsync(bool? active = null)
        {
            var query = _context.Departments
                .Include(d => d.Employees)
                .AsQueryable();

            if (active.HasValue)
            {
                query = query.Where(d => d.Active == active);
            }

            return await query
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetActiveDepartmentsAsync()
        {
            return await _context.Departments
                .Where(d => d.Active)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null) return false;

                // Verificar que no tenga empleados activos
                var hasActiveEmployees = await _context.Employees
                    .AnyAsync(e => e.DepartmentId == id && e.Active);
                
                if (hasActiveEmployees)
                {
                    throw new InvalidOperationException("No se puede eliminar un departamento con empleados activos");
                }

                // Soft delete
                department.Active = false;
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Departamento eliminado (soft): {DepartmentId}", department.Id);
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
                if (department == null) return false;

                department.Active = true;
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
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
                if (department == null) return false;

                // Verificar que no tenga empleados activos
                var hasActiveEmployees = await _context.Employees
                    .AnyAsync(e => e.DepartmentId == id && e.Active);
                
                if (hasActiveEmployees)
                {
                    throw new InvalidOperationException("No se puede desactivar un departamento con empleados activos");
                }

                department.Active = false;
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando departamento {DepartmentId}", id);
                throw;
            }
        }

        public async Task<int> GetEmployeeCountAsync(int departmentId)
        {
            return await _context.Employees
                .CountAsync(e => e.DepartmentId == departmentId && e.Active);
        }

        public async Task<bool> CanDeleteDepartmentAsync(int departmentId)
        {
            var employeeCount = await GetEmployeeCountAsync(departmentId);
            return employeeCount == 0;
        }

        public async Task<bool> IsDepartmentNameUniqueAsync(string name, int? excludeId = null)
        {
            var query = _context.Departments.Where(d => d.Name == name);
            
            if (excludeId.HasValue)
            {
                query = query.Where(d => d.Id != excludeId);
            }

            return !await query.AnyAsync();
        }
    }
}