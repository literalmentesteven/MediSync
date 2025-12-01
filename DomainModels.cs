using System.Text.Json.Serialization;

namespace MediSync.Models
{
    public class LoginRequest { public string IdUsuario { get; set; } = string.Empty; public string Contraseña { get; set; } = string.Empty; }
    // Agregado IdUsuario a LoginResponse
    public class LoginResponse { public string Token { get; set; } = string.Empty; public string Rol { get; set; } = string.Empty; public string NombreUsuario { get; set; } = string.Empty; public string IdUsuario { get; set; } = string.Empty; }

    public class Usuario { public int Id { get; set; } public string IdUsuario { get; set; }=""; public string NombreCompleto { get; set; }=""; public string Rol { get; set; }=""; public DateTime FechaNacimiento { get; set; } public string Telefono { get; set; }=""; public string Info { get; set; }=""; public string Especialidad { get; set; }=""; public string FotoUrl { get; set; }=""; public int Edad => DateTime.Now.Year - FechaNacimiento.Year; }
    public class Paciente { public int Id { get; set; } public string NombreCompleto { get; set; }=""; public string IdLegal { get; set; }=""; public int Edad { get; set; } public double Altura { get; set; } public double Peso { get; set; } public string Sexo { get; set; }=""; public string Direccion { get; set; }=""; public string Telefono { get; set; }=""; public string EstadoCita { get; set; }="Sin Cita"; public DateTime HoraCita { get; set; } public string DoctorAsignado { get; set; }=""; public string HistoriaClinica { get; set; }=""; [JsonIgnore] public bool TieneCita => EstadoCita != "Sin Cita"; [JsonIgnore] public Color EstadoColor => EstadoCita switch { "Asistió" => Colors.Green, "Ausente" => Colors.Red, "Pendiente" => Colors.Orange, _ => Colors.Gray }; }
    
    // EXAMEN ACTUALIZADO
    public class Examen { 
        public int Id { get; set; } 
        public string PacienteNombre { get; set; }=""; 
        public string TipoExamen { get; set; }=""; 
        public DateTime Fecha { get; set; } 
        public string Estado { get; set; }="Pendiente"; 
        public bool EsCritico { get; set; } 
        public bool EsUrgente { get; set; }
        public string DatosResultado { get; set; }=""; 
        
        // Campos de control
        public string DoctorSolicitanteId { get; set; }="";
        public string NombreDoctor { get; set; }="";
        public string ArchivoPdfUrl { get; set; }=""; // Simula el PDF

        [JsonIgnore] public bool TienePdf => !string.IsNullOrEmpty(ArchivoPdfUrl);
        [JsonIgnore] public Color ColorEstado => EsCritico ? Colors.Red : (Estado == "Enviado" ? Color.FromArgb("#00B2CA") : (Estado == "Completado" ? Colors.Green : Colors.Gray));
        [JsonIgnore] public string IconoEstado => EsCritico ? "⚠️" : (Estado == "Enviado" ? "📩" : (Estado == "Completado" ? "✅" : "⏳"));
    }

    public class TipoExamenCheck { public string Nombre { get; set; } = ""; public bool IsSelected { get; set; } }
    public class CreateUserRequest { public string NombreCompleto { get; set; }=""; public string Password { get; set; }=""; public string Rol { get; set; }=""; public DateTime FechaNacimiento { get; set; } public string Telefono { get; set; }=""; public string Info { get; set; }=""; public string Especialidad { get; set; }=""; }
    public class UpdateProfileRequest { public string Telefono { get; set; }=""; public string Info { get; set; }=""; public string FotoUrl { get; set; }=""; }
    public class ResultadoDetalle { public string Parametro { get; set; }=""; public string Valor { get; set; }=""; public string Referencia { get; set; }=""; public Color ColorValor { get; set; } = Colors.Black; }
    public class CalendarDay { public string DayNumber { get; set; } = ""; public DateTime? Date { get; set; } public Color BgColor { get; set; } = Colors.Transparent; public Color TextColor { get; set; } = Colors.Black; public bool IsSelected { get; set; } }
}