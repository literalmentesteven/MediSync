using ClinicalAppointments.Api.Data;
using ClinicalAppointments.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/resultados-laboratorio")]
    [Authorize] // Proteger por defecto
    public class ResultadosLaboratorioController : ControllerBase
    {
        private readonly ClinicalContext _context;

        public ResultadosLaboratorioController(ClinicalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// (Admin, Doctor) Obtiene todos los resultados de un paciente específico.
        /// </summary>
        [HttpGet("paciente/{idPaciente}")]
        [Authorize(Policy = "AdminOrDoctor")] // Doctores y Admin pueden ver
        public async Task<IActionResult> GetResultadosPorPaciente(int idPaciente)
        {
            var resultados = await _context.ResultadosLaboratorios
                .Where(r => r.IdPaciente == idPaciente)
                .OrderByDescending(r => r.Fecha) // Ordenar por fecha
                .ToListAsync();

            return Ok(resultados);
        }

        /// <summary>
        /// (Laboratorio) Sube un nuevo resultado de examen.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "LaboratorioOnly")] // Solo personal de lab puede crear
        public async Task<IActionResult> CrearResultado([FromBody] CreateResultadoDto dto)
        {
            // Validar que el paciente exista
            var paciente = await _context.Pacientes.FindAsync(dto.IdPaciente);
            if (paciente == null)
            {
                return NotFound(new { Mensaje = "El paciente especificado no existe." });
            }

            // Mapear DTO a la entidad generada 'ResultadosLaboratorio'
            var nuevoResultado = new ResultadosLaboratorio
            {
                IdPaciente = dto.IdPaciente,
                TipoExamen = dto.TipoExamen,
                Resultado = dto.Resultado,
                Fecha = dto.Fecha, // Asumir formato ISO 8601
                Estado = dto.Estado,
                IdCitaRelacionada = dto.IdCitaRelacionada
            };

            _context.ResultadosLaboratorios.Add(nuevoResultado);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetResultadosPorPaciente), new { idPaciente = nuevoResultado.IdPaciente }, nuevoResultado);
        }
    }
}