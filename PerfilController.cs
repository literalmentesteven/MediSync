using ClinicalAppointments.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // CUALQUIER usuario logueado puede ver SU perfil
    public class PerfilController : ControllerBase
    {
        private readonly ClinicalContext _context;

        public PerfilController(ClinicalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la información del perfil del usuario actualmente logueado.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMiPerfil()
        {
            // Obtenemos el ID de usuario (string) del token JWT
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Convertimos el ID (que es un long/int en tu BD)
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
            {
                return Unauthorized();
            }

            // Buscamos en la tabla 'Usuarios' (la nueva clase generada)
            var usuario = await _context.Usuarios
                .Select(u => new { u.IdUsuario, u.Username, u.NombreCompleto, u.Rol }) // Quitamos el Hash del password
                .FirstOrDefaultAsync(u => u.IdUsuario == userId);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            return Ok(usuario);
        }

        // (Aquí iría un [HttpPut("me")] para que el usuario actualice su contraseña o nombre)
    }
}