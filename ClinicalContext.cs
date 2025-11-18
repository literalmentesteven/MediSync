using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ClinicalAppointments.Api.Data;

public partial class ClinicalContext : DbContext
{
    public ClinicalContext()
    {
    }

    public ClinicalContext(DbContextOptions<ClinicalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cita> Citas { get; set; }
    public virtual DbSet<Doctore> Doctores { get; set; }
    public virtual DbSet<HorariosDoctor> HorariosDoctors { get; set; }
    public virtual DbSet<Paciente> Pacientes { get; set; }
    public virtual DbSet<ResultadosLaboratorio> ResultadosLaboratorios { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }

    // ¡EL MÉTODO OnConfiguring QUE CAUSABA EL ERROR HA SIDO ELIMINADO!
    // (Ya no está aquí)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.IdCita);

            entity.ToTable("citas");

            entity.Property(e => e.IdCita).HasColumnName("id_cita");

            entity.Property(e => e.DuracionMinutos)
                .IsRequired()
                .HasColumnName("duracion_minutos");

            entity.Property(e => e.Estado)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("estado");

            entity.Property(e => e.FechaHora)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("fecha_hora");

            entity.Property(e => e.IdDoctor).HasColumnName("id_doctor");

            entity.Property(e => e.IdPaciente).HasColumnName("id_paciente");

            entity.Property(e => e.Motivo)
                .HasColumnType("TEXT")
                .HasColumnName("motivo");

            entity.HasOne(d => d.IdDoctorNavigation).WithMany(p => p.Cita)
                .HasForeignKey(d => d.IdDoctor)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IdPacienteNavigation).WithMany(p => p.Cita).HasForeignKey(d => d.IdPaciente);
        });

        modelBuilder.Entity<Doctore>(entity =>
        {
            entity.HasKey(e => e.IdDoctor);

            entity.ToTable("doctores");

            entity.Property(e => e.IdDoctor).HasColumnName("id_doctor");

            entity.Property(e => e.Cargo)
                .HasColumnType("TEXT")
                .HasColumnName("cargo");

            entity.Property(e => e.Edad).HasColumnName("edad");

            entity.Property(e => e.Especialidad)
                .HasColumnType("TEXT")
                .HasColumnName("especialidad");

            entity.Property(e => e.NombreCompleto)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("nombre_completo");
        });

        modelBuilder.Entity<HorariosDoctor>(entity =>
        {
            entity.HasKey(e => e.IdHorario);

            entity.ToTable("horarios_doctor");

            entity.Property(e => e.IdHorario).HasColumnName("id_horario");

            entity.Property(e => e.Descripcion)
                .HasColumnType("TEXT")
                .HasColumnName("descripcion");

            entity.Property(e => e.Fin)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("fin");

            entity.Property(e => e.IdDoctor).HasColumnName("id_doctor");

            entity.Property(e => e.Inicio)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("inicio");

            entity.HasOne(d => d.IdDoctorNavigation).WithMany(p => p.HorariosDoctors).HasForeignKey(d => d.IdDoctor);
        });

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.HasKey(e => e.IdPaciente);

            entity.ToTable("pacientes");

            entity.Property(e => e.IdPaciente).HasColumnName("id_paciente");

            entity.Property(e => e.AlturaM).HasColumnName("altura_m");

            entity.Property(e => e.Direccion)
                .HasColumnType("TEXT")
                .HasColumnName("direccion");

            entity.Property(e => e.Edad).HasColumnName("edad");

            entity.Property(e => e.NombreCompleto)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("nombre_completo");

            entity.Property(e => e.PesoKg).HasColumnName("peso_kg");

            entity.Property(e => e.Sexo)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("sexo");

            entity.Property(e => e.Telefono)
                .HasColumnType("TEXT")
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<ResultadosLaboratorio>(entity =>
        {
            entity.HasKey(e => e.IdResultado);

            entity.ToTable("resultados_laboratorio");

            entity.Property(e => e.IdResultado).HasColumnName("id_resultado");

            entity.Property(e => e.Estado)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("estado");

            entity.Property(e => e.Fecha)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("fecha");

            entity.Property(e => e.IdCitaRelacionada).HasColumnName("id_cita_relacionada");

            entity.Property(e => e.IdPaciente).HasColumnName("id_paciente");

            entity.Property(e => e.Resultado)
                .HasColumnType("TEXT")
                .HasColumnName("resultado");

            entity.Property(e => e.TipoExamen)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("tipo_examen");

            entity.HasOne(d => d.IdCitaRelacionadaNavigation).WithMany(p => p.ResultadosLaboratorios).HasForeignKey(d => d.IdCitaRelacionada);

            entity.HasOne(d => d.IdPacienteNavigation).WithMany(p => p.ResultadosLaboratorios).HasForeignKey(d => d.IdPaciente);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Username, "IX_usuarios_username").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");

            entity.Property(e => e.NombreCompleto)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("nombre_completo");

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("password_hash");

            entity.Property(e => e.Rol)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("rol");

            entity.Property(e => e.Username)
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}