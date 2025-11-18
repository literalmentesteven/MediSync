using System;
using System.Collections.Generic;

namespace ClinicalAppointments.Api.Data;

public partial class Cita
{
    public int IdCita { get; set; }

    public int IdPaciente { get; set; }

    public int? IdDoctor { get; set; }

    public string FechaHora { get; set; } = null!;

    public int DuracionMinutos { get; set; }

    public string Estado { get; set; } = null!;

    public string? Motivo { get; set; }

    public virtual Doctore? IdDoctorNavigation { get; set; }

    public virtual Paciente IdPacienteNavigation { get; set; } = null!;

    public virtual ICollection<ResultadosLaboratorio> ResultadosLaboratorios { get; set; } = new List<ResultadosLaboratorio>();
}
