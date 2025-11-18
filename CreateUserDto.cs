using System.ComponentModel.DataAnnotations;

namespace ClinicalAppointments.Api.DTOs
{
    /// <summary>
    /// DTO para crear un nuevo usuario en la tabla 'Usuarios'
    /// </summary>
    public class CreateUserDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string NombreCompleto { get; set; }

        [Required]
        public string Rol { get; set; } // "superadmin", "personal_admin", "doctor", "laboratorio"
    }
}