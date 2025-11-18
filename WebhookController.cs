using ClinicalAppointments.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicalAppointments.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly ClinicalContext _context;

        // Aunque no lo usemos ahora, es bueno tener el contexto
        // para buscar al paciente por su teléfono en el futuro.
        public WebhookController(ClinicalContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Endpoint para recibir notificaciones de WhatsApp.
        /// </summary>
        [HttpPost("whatsapp")]
        public async Task<IActionResult> RecibirMensajeWhatsApp([FromBody] object payload)
        {
            // --- PASOS A SEGUIR ---
            // 1. Validar que la petición venga de tu proveedor de WhatsApp.

            // 2. Parsear el 'payload' para obtener el mensaje y el teléfono del usuario.
            //    string telefonoUsuario = payload...
            //    string mensaje = payload...

            // 3. Buscar al paciente por 'telefonoUsuario' en la nueva BD
            //    var paciente = await _context.Pacientes
            //                          .FirstOrDefaultAsync(p => p.Telefono == telefonoUsuario);

            // 4. Procesar el 'mensaje' (IA, NLU, o lógica de botones).

            // 5. Agendar la cita si es necesario.

            Console.WriteLine("Webhook de WhatsApp recibido.");
            Console.WriteLine(payload?.ToString());

            // Devolver un 200 OK para que el proveedor sepa que recibiste el mensaje.
            return Ok();
        }
    }
}