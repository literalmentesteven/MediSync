using ClinicalAppointments.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Proteger todos los endpoints de citas
    public class CitasController : ControllerBase
    {
        private readonly ClinicalContext _context;

        public CitasController(ClinicalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene las citas para el calendario, filtradas por rango de fechas.
        /// Tu BD usa texto ISO 8601 ("YYYY-MM-DD HH:MM:SS")
        /// </summary>
        /// <param name="start">Fecha de inicio (ej. "2025-11-01 00:00:00")</param>
        /// <param name="end">Fecha de fin (ej. "2025-12-01 00:00:00")</param>
        [HttpGet("calendario")]
        public async Task<IActionResult> GetCitasParaCalendario([FromQuery] string start, [FromQuery] string end)
        {
            // Como tu BD guarda fechas como TEXTO, la consulta es más compleja
            // pero EF Core puede manejarla si el formato es ISO 8601
            var citas = await _context.Citas
                .Where(c => c.FechaHora.CompareTo(start) >= 0 && c.FechaHora.CompareTo(end) < 0)
                .Include(c => c.IdDoctorNavigation) // Cargar datos del Doctor
                .Include(c => c.IdPacienteNavigation) // Cargar datos del Paciente
                .Select(c => new
                {
                    // Formato para un calendario como FullCalendar.js
                    id = c.IdCita,
                    title = $"Cita: {c.IdPacienteNavigation.NombreCompleto} (Dr. {c.IdDoctorNavigation.NombreCompleto})",
                    start = c.FechaHora, // Tu BD ya usa formato ISO 8601
                    // Fin (calculado)
                    end = DateTime.Parse(c.FechaHora, CultureInfo.InvariantCulture).AddMinutes(c.DuracionMinutos).ToString("yyyy-MM-dd HH:mm:ss"),
                    motivo = c.Motivo,
                    estado = c.Estado,
                    pacienteId = c.IdPaciente,
                    doctorId = c.IdDoctor
                })
                .ToListAsync();

            return Ok(citas);
        }

        /// <summary>
        /// Crea una nueva cita.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Solo admins pueden crear citas
        public async Task<IActionResult> CrearCita([FromBody] Cita nuevaCita)
        {
            // (Aquí faltaría validación: checar que el doctor exista, paciente exista,
            // y que la hora no esté ocupada)

            // Asegurarse de que el estado inicial sea 'programada'
            nuevaCita.Estado = "programada";

            _context.Citas.Add(nuevaCita);
            await _context.SaveChangesAsync();
            return Ok(nuevaCita);
        }
    }
}