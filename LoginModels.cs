using System.ComponentModel.DataAnnotations;

namespace ClinicalAppointments.Api.DTOs
{
    /// <summary>
    /// Lo que el usuario envía para iniciar sesión.
    /// ¡OJO! Se actualizó a "Username" para coincidir con tu BD.
    /// </summary>
    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; } // <-- CAMBIADO (antes era NombreUsuario)

        [Required]
        public string Password { get; set; }
    }

    /// <summary>
    /// Lo que el servidor responde si el login es exitoso.
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string NombreCompleto { get; set; }
        public string Rol { get; set; }
    }
}