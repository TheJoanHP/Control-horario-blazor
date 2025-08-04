namespace Shared.Models.Enums
{
    /// <summary>
    /// Tipos de registro de tiempo
    /// </summary>
    public enum RecordType
    {
        /// <summary>
        /// Entrada al trabajo
        /// </summary>
        CheckIn = 0,

        /// <summary>
        /// Salida del trabajo
        /// </summary>
        CheckOut = 1,

        /// <summary>
        /// Inicio de descanso/pausa
        /// </summary>
        BreakStart = 2,

        /// <summary>
        /// Fin de descanso/pausa
        /// </summary>
        BreakEnd = 3
    }
}