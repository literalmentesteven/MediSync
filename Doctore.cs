using System;
using System.Collections.Generic;

namespace ClinicalAppointments.Api.Data;

public partial class Doctore
{
    public int IdDoctor { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public int? Edad { get; set; }

    public string? Especialidad { get; set; }

    public string? Cargo { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<HorariosDoctor> HorariosDoctors { get; set; } = new List<HorariosDoctor>();
}
