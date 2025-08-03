using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Core;

namespace Company.Admin.Server.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly CompanyDbContext _context;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            CompanyDbContext context,
            ILogger<DepartmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Department> CreateDepartmentAsync(string name, string? description = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre del departamento es requerido");

                // Verificar unicidad del nombre (a nivel de empresa se valida en el controlador)
                var department = new Department
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                return department;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando departamento {Name}", name);
                throw;
            }
        }

        public async Task<Department> UpdateDepartmentAsync(int id, string name, string? description = null)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    throw new KeyNotFoundException($"Departamento con ID {id} no encontrado");

                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre del departamento es requerido");

                department.Name = name.Trim();
                department.Description = description?.Trim();

                await _context.SaveChangesAsync();

                return department;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando departamento {Id}", id);
                throw;
            }
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            try
            {
                return await _context.Departments
                    .Include(d => d.Company)
                    .Include(d => d.Employees)
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo departamento {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Department>> GetDepartmentsAsync(bool? active = null)
        {
            try
            {
                var query = _context.Departments
                    .Include(d => d.Company)
                    .AsQueryable();

                if (active.HasValue)
                    query = query.Where(d => d.Active == active.Value);

                return await query
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo departamentos");
                throw;
            }
        }

        public async Task<IEnumerable<Department>> GetActiveDepartmentsAsync()
        {
            return await GetDepartmentsAsync(active: true);
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
                    // Solo desactivar si tiene empleados
                    department.Active = false;
                }
                else
                {
                    // Eliminar completamente si no tiene empleados
                    _context.Departments.Remove(department);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando departamento {Id}", id);
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
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando departamento {Id}", id);
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
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando departamento {Id}", id);
                throw;
            }
        }

        public async Task<int> GetEmployeeCountAsync(int departmentId)
        {
            try
            {
                return await _context.Employees
                    .CountAsync(e => e.DepartmentId == departmentId && e.Active);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo conteo de empleados del departamento {Id}", departmentId);
                throw;
            }
        }

        public async Task<bool> CanDeleteDepartmentAsync(int departmentId)
        {
            try
            {
                var employeeCount = await GetEmployeeCountAsync(departmentId);
                return employeeCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando si se puede eliminar departamento {Id}", departmentId);
                throw;
            }
        }

        public async Task<bool> IsDepartmentNameUniqueAsync(string name, int? excludeId = null)
        {
            try
            {
                var query = _context.Departments.Where(d => d.Name == name);

                if (excludeId.HasValue)
                    query = query.Where(d => d.Id != excludeId);

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando unicidad del nombre de departamento {Name}", name);
                throw;
            }
        }
    }
}