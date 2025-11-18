using System.ComponentModel.DataAnnotations;

namespace ClinicalAppointments.Api.DTOs
{
    /// <summary>
    /// DTO para crear un nuevo resultado en la tabla 'resultados_laboratorio'
    /// </summary>
    public class CreateResultadoDto
    {
        [Required]
        public int IdPaciente { get; set; } // <-- CORREGIDO (era 'long')

        [Required]
        public string TipoExamen { get; set; } = string.Empty;

        public string? Resultado { get; set; }

        [Required]
        public string Fecha { get; set; } = string.Empty; // Tu BD espera "YYYY-MM-DD" o "YYYY-MM-DD HH:MM:SS"

        [Required]
        public string Estado { get; set; } = string.Empty; // "pendiente", "listo", "cancelado"

        public int? IdCitaRelacionada { get; set; } // <-- CORREGIDO (era 'long?')
    }
}