using ClinicalAppointments.Api.Data;
using ClinicalAppointments.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // ¡Proteger TODA la clase por defecto!
    public class UsuariosController : ControllerBase
    {
        private readonly ClinicalContext _context;

        public UsuariosController(ClinicalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// (Admin) Obtiene la lista de todos los usuarios que pueden iniciar sesión.
        /// (Este sigue protegido por el [Authorize] de la clase)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new { u.IdUsuario, u.Username, u.NombreCompleto, u.Rol })
                .ToListAsync();
            return Ok(usuarios);
        }

        /// <summary>
        /// (Admin) Crea un nuevo usuario para iniciar sesión (Admin, Doctor, Lab).
        /// </summary>
        [HttpPost]
        [AllowAnonymous] // <-- ¡¡ESTA ES LA CORRECCIÓN!!
        public async Task<IActionResult> CrearUsuario([FromBody] CreateUserDto userDto)
        {
            var rolesPermitidos = new[] { "superadmin", "personal_admin", "doctor", "laboratorio" };
            if (!rolesPermitidos.Contains(userDto.Rol.ToLower()))
            {
                return BadRequest(new { Mensaje = "El rol especificado no es válido." });
            }

            var usuarioExistente = await _context.Usuarios
                .AnyAsync(u => u.Username == userDto.Username);

            if (usuarioExistente)
            {
                return BadRequest(new { Mensaje = "El username ya existe." });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var nuevoUsuario = new Usuario
            {
                Username = userDto.Username,
                PasswordHash = passwordHash,
                NombreCompleto = userDto.NombreCompleto,
                Rol = userDto.Rol.ToLower()
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuarios), new { id = nuevoUsuario.IdUsuario }, nuevoUsuario);
        }
    }
}