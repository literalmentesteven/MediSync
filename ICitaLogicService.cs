namespace ClinicalAppointments.Api.Services
{
    /// <summary>
    /// Interfaz para la lógica de negocio de las citas.
    /// </summary>
    public interface ICitaLogicService
    {
        /// <summary>
        /// Confirma la asistencia de un paciente a una cita.
        /// Solo se puede confirmar dentro de la ventana de tiempo permitida.
        /// </summary>
        /// <param name="citaId">El ID de la cita a confirmar.</param>
        /// <returns>Resultado de la operación.</returns>
        Task<ConfirmarAsistenciaResult> ConfirmarAsistencia(long citaId);

        /// <summary>
        /// Busca y marca automáticamente todas las citas pendientes
        /// que han pasado su ventana de tiempo como "NoShow" (Canceladas).
        /// </summary>
        Task MarcarCitasAusentes();
    }

    public class ConfirmarAsistenciaResult
    {
        public bool Exito { get; set; }
        public string MensajeError { get; set; } = string.Empty;
    }
}