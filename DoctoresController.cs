using ClinicalAppointments.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctoresController : ControllerBase
    {
        private readonly ClinicalContext _context;

        public DoctoresController(ClinicalContext context)
        {
            _context = context;
        }

        // GET: api/Doctores
        /// <summary>
        /// Obtiene un listado de todos los doctores. (Público o Autorizado)
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Permitir que cualquiera vea la lista de doctores
        public async Task<IActionResult> GetDoctores()
        {
            // Usa la nueva clase 'Doctore' (generada por scaffold)
            var doctores = await _context.Doctores
                .Select(d => new { d.IdDoctor, d.NombreCompleto, d.Especialidad, d.Cargo })
                .ToListAsync();
            return Ok(doctores);
        }

        // POST: api/Doctores
        /// <summary>
        /// (Admin) Añade un nuevo doctor al directorio.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Solo Admins
        public async Task<IActionResult> CrearDoctor([FromBody] Doctore doctor)
        {
            // Nota: Esto NO crea un 'Usuario' para login.
            // Solo añade un doctor al directorio público.

            _context.Doctores.Add(doctor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDoctores), new { id = doctor.IdDoctor }, doctor);
        }

        // DELETE: api/Doctores/5
        /// <summary>
        /// (Admin) Elimina un doctor del directorio.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EliminarDoctor(int id)
        {
            var doctor = await _context.Doctores.FindAsync(id);
            if (doctor == null) return NotFound();

            _context.Doctores.Remove(doctor);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}