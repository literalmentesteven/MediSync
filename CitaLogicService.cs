using ClinicalAppointments.Api.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicalAppointments.Api.Services
{
    /// <summary>
    /// Implementación de la lógica de negocio de las citas.
    /// ADAPTADA para SQLite y estados de texto.
    /// </summary>
    public class CitaLogicService : ICitaLogicService
    {
        private readonly ClinicalContext _context;
        private const int VentanaMinutosConfirmacion = 15;

        // Estados de tu script SQL
        private const string CitaProgramada = "programada";
        private const string CitaConfirmada = "confirmada";
        private const string CitaCompletada = "completada";
        private const string CitaCancelada = "cancelada"; // Usaremos esto para 'NoShow'

        public CitaLogicService(ClinicalContext context)
        {
            _context = context;
        }

        public async Task<ConfirmarAsistenciaResult> ConfirmarAsistencia(long citaId)
        {
            var cita = await _context.Citas.FindAsync(citaId);
            if (cita == null)
            {
                return new ConfirmarAsistenciaResult { Exito = false, MensajeError = "Cita no encontrada." };
            }

            // Solo se puede confirmar si está 'programada' o 'confirmada'
            if (cita.Estado != CitaProgramada && cita.Estado != CitaConfirmada)
            {
                return new ConfirmarAsistenciaResult { Exito = false, MensajeError = $"La cita ya está en estado: {cita.Estado}." };
            }

            var now = DateTime.UtcNow;

            // Convertir la fecha de TEXTO (ISO 8601) a DateTime
            if (!DateTime.TryParse(cita.FechaHora, out var horaInicio))
            {
                return new ConfirmarAsistenciaResult { Exito = false, MensajeError = "Formato de fecha inválido en la Base de Datos." };
            }

            var horaLimite = horaInicio.AddMinutes(VentanaMinutosConfirmacion);

            // Lógica de los 15 minutos:
            // Si la hora actual está DENTRO de la ventana (hora de inicio a hora límite)
            if (now >= horaInicio && now <= horaLimite)
            {
                cita.Estado = CitaCompletada; // Marcamos como "completada" (el paciente llegó)
                await _context.SaveChangesAsync();
                return new ConfirmarAsistenciaResult { Exito = true };
            }

            // Si la hora actual es DESPUÉS de la hora límite
            if (now > horaLimite)
            {
                cita.Estado = CitaCancelada; // Marcamos como "cancelada" (equivale a 'NoShow')
                await _context.SaveChangesAsync();
                return new ConfirmarAsistenciaResult { Exito = false, MensajeError = "Tiempo límite excedido. La cita se marca como 'Cancelada'." };
            }

            // Si la hora actual es ANTES de la hora de inicio
            return new ConfirmarAsistenciaResult { Exito = false, MensajeError = "Aún no es la hora de la cita. No se puede confirmar la asistencia." };
        }

        /// <summary>
        /// Esto se debe ejecutar en segundo plano (ej. cada hora)
        /// </summary>
        public async Task MarcarCitasAusentes()
        {
            var now = DateTime.UtcNow;

            // Obtenemos citas que aún están 'programadas' o 'confirmadas'
            var citasPendientes = await _context.Citas
                .Where(c => c.Estado == CitaProgramada || c.Estado == CitaConfirmada)
                .ToListAsync();

            var citasParaCancelar = new List<Cita>();
            foreach (var cita in citasPendientes)
            {
                // Convertimos la fecha de texto
                if (DateTime.TryParse(cita.FechaHora, out var horaInicio))
                {
                    var horaLimite = horaInicio.AddMinutes(VentanaMinutosConfirmacion);
                    // Si la hora límite ya pasó, se marca como 'NoShow' (Cancelada)
                    if (now > horaLimite)
                    {
                        cita.Estado = CitaCancelada;
                        citasParaCancelar.Add(cita);
                    }
                }
            }

            if (citasParaCancelar.Any())
            {
                // Guardamos todos los cambios a la BD de una vez
                await _context.SaveChangesAsync();
            }
        }
    }
}