namespace Shared.Models.Enums
{
    /// <summary>
    /// Estados de las solicitudes de vacaciones
    /// </summary>
    public enum VacationStatus
    {
        /// <summary>
        /// Solicitud pendiente de aprobación
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Solicitud aprobada
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Solicitud rechazada
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Solicitud cancelada
        /// </summary>
        Cancelled = 3
    }
}