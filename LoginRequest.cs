namespace MediSync.Models
{
    // Esta clase representa el JSON que TÚ envías
    public class LoginRequest
    {
        // ¡IMPORTANTE! Estos nombres (ej. IdUsuario, Contraseña)
        // deben ser EXACTAMENTE iguales a como los espera Roberto en su API.
        // Pregúntale a él. (Quizás es "username" o "password" en minúscula)
        public string IdUsuario { get; set; }
        public string Contraseña { get; set; }
    }
}