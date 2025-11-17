namespace MediSync.Models
{
    // Esta clase representa el JSON que Roberto te devuelve
    // cuando el login es exitoso.
    public class LoginResponse
    {
        // Estos nombres también deben ser exactos
        public string Token { get; set; }
        public string Rol { get; set; }
        public string NombreUsuario { get; set; }
    }
}