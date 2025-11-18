using ClinicalAppointments.Api.Data;
using ClinicalAppointments.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ClinicalContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ClinicalContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Endpoint de inicio de sesión (adaptado a tu BD).
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            // 1. Encontrar al usuario por su 'Username'
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

            if (usuario == null)
            {
                return Unauthorized(new { Mensaje = "Credenciales inválidas." });
            }

            // 2. Verificar la contraseña (usando el 'PasswordHash' de tu BD)
            bool esPasswordValido = BCrypt.Net.BCrypt.Verify(loginRequest.Password, usuario.PasswordHash);

            if (!esPasswordValido)
            {
                return Unauthorized(new { Mensaje = "Credenciales inválidas." });
            }

            // 3. Generar el Token
            var token = GenerarJwtToken(usuario);

            return Ok(new LoginResponseDto
            {
                Token = token,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol
            });
        }

        private string GenerarJwtToken(Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Username),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}