namespace Shared.Models.Enums
{
    /// <summary>
    /// Tipos de licencia en el sistema
    /// </summary>
    public enum LicenseType
    {
        /// <summary>
        /// Licencia de prueba (30 días, 5 empleados, funcionalidades básicas)
        /// </summary>
        Trial = 0,

        /// <summary>
        /// Licencia básica (10 empleados, reportes básicos)
        /// </summary>
        Basic = 1,

        /// <summary>
        /// Licencia profesional (50 empleados, reportes avanzados, API)
        /// </summary>
        Professional = 2,

        /// <summary>
        /// Licencia empresarial (empleados ilimitados, todas las funcionalidades)
        /// </summary>
        Enterprise = 3
    }
}