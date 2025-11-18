using ClinicalAppointments.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOrDoctor")] // Usa la política de Program.cs
    public class PacientesController : ControllerBase
    {
        private readonly ClinicalContext _context;

        public PacientesController(ClinicalContext context)
        {
            _context = context;
        }

        // GET: api/Pacientes
        /// <summary>
        /// Obtiene un listado de todos los pacientes.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPacientes()
        {
            // Usa la nueva clase 'Paciente' (generada por scaffold)
            var pacientes = await _context.Pacientes
                .Select(p => new { p.IdPaciente, p.NombreCompleto, p.Telefono })
                .ToListAsync();
            return Ok(pacientes);
        }

        // GET: api/Pacientes/5
        /// <summary>
        /// Obtiene los detalles de un paciente, incluyendo sus citas y su historial.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPacienteDetalle(int id)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Cita) // Cargar citas (el scaffold puede haberlo llamado 'Cita' o 'Citas')
                .FirstOrDefaultAsync(p => p.IdPaciente == id);

            if (paciente == null) return NotFound();

            return Ok(paciente);
        }

        // POST: api/Pacientes
        /// <summary>
        /// (Admin) Crea un nuevo paciente.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CrearPaciente([FromBody] Paciente paciente)
        {
            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPacienteDetalle), new { id = paciente.IdPaciente }, paciente);
        }
    }
}