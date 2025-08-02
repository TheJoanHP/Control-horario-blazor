using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Core;

namespace Company.Admin.Server.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly CompanyDbContext _context;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(CompanyDbContext context, ILogger<DepartmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Department> CreateDepartmentAsync(string name, string? description = null)
        {
            // Validar nombre único
            if (!await IsDepartmentNameUniqueAsync(name))
            {
                throw new InvalidOperationException("Ya existe un departamento con este nombre");
            }

            // Obtener la empresa (primera que esté activa, ya que estamos en el contexto del tenant)
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Active);
            if (company == null)
            {
                throw new InvalidOperationException("No se encontró una empresa activa para este tenant");
            }

            var department = new Department
            {
                CompanyId = company.Id,
                Name = name.Trim(),
                Description = description?.Trim(),
                Active = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Departamento creado: {DepartmentId} - {DepartmentName}", department.Id, department.Name);
            return department;
        }

        public async Task<Department> UpdateDepartmentAsync(int id, string name, string? description = null)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                throw new ArgumentException($"Departamento con ID {id} no encontrado");
            }

            // Validar nombre único si se está cambiando
            if (name.Trim() != department.Name && !await IsDepartmentNameUniqueAsync(name, id))
            {
                throw new InvalidOperationException("Ya existe un departamento con este nombre");
            }

            department.Name = name.Trim();
            department.Description = description?.Trim();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Departamento actualizado: {DepartmentId} - {DepartmentName}", department.Id, department.Name);
            return department;
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            return await _context.Departments
                .Include(d => d.Company)
                .Include(d => d.Employees.Where(e => e.Active))
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsAsync(bool? active = null)
        {
            var query = _context.Departments
                .Include(d => d.Company)
                .AsQueryable();

            if (active.HasValue)
            {
                query = query.Where(d => d.Active == active.Value);
            }

            return await query
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Department>> GetActiveDepartmentsAsync()
        {
            return await GetDepartmentsAsync(active: true);
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return false;

            // Verificar si se puede eliminar (no tiene empleados activos)
            if (!await CanDeleteDepartmentAsync(id))
            {
                throw new InvalidOperationException("No se puede eliminar el departamento porque tiene empleados asignados");
            }

            // Soft delete
            department.Active = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Departamento desactivado: {DepartmentId} - {DepartmentName}", department.Id, department.Name);
            return true;
        }

        public async Task<bool> ActivateDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return false;

            department.Active = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Departamento activado: {DepartmentId} - {DepartmentName}", department.Id, department.Name);
            return true;
        }

        public async Task<bool> DeactivateDepartmentAsync(int id)
        {
            return await DeleteDepartmentAsync(id);
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
            var query = _context.Departments.Where(d => d.Name.ToLower() == name.ToLower().Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(d => d.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }
    }
}