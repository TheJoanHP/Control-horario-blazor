namespace Shared.Models.Enums
{
    /// <summary>
    /// Roles de usuario en el sistema
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Super administrador del sistema Sphere (acceso total)
        /// </summary>
        SuperAdmin = 0,

        /// <summary>
        /// Administrador de empresa/tenant (gestión de empleados)
        /// </summary>
        CompanyAdmin = 1,

        /// <summary>
        /// Supervisor de departamento (supervisión de empleados)
        /// </summary>
        Supervisor = 2,

        /// <summary>
        /// Empleado regular (solo fichaje y consultas propias)
        /// </summary>
        Employee = 3
    }
}