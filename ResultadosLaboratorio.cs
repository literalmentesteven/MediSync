using System;
using System.Collections.Generic;

namespace ClinicalAppointments.Api.Data;

public partial class ResultadosLaboratorio
{
    public int IdResultado { get; set; }

    public int IdPaciente { get; set; }

    public string TipoExamen { get; set; } = null!;

    public string? Resultado { get; set; }

    public string Fecha { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public int? IdCitaRelacionada { get; set; }

    public virtual Cita? IdCitaRelacionadaNavigation { get; set; }

    public virtual Paciente IdPacienteNavigation { get; set; } = null!;
}
