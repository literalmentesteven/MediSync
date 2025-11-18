using System;
using System.Collections.Generic;

namespace ClinicalAppointments.Api.Data;

public partial class HorariosDoctor
{
    public int IdHorario { get; set; }

    public int IdDoctor { get; set; }

    public string Inicio { get; set; } = null!;

    public string Fin { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual Doctore IdDoctorNavigation { get; set; } = null!;
}
