using System;
using System.Collections.Generic;

namespace ClinicalAppointments.Api.Data;

public partial class Paciente
{
    public int IdPaciente { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public int Edad { get; set; }

    public string Sexo { get; set; } = null!;

    public double? PesoKg { get; set; }

    public double? AlturaM { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<ResultadosLaboratorio> ResultadosLaboratorios { get; set; } = new List<ResultadosLaboratorio>();
}
