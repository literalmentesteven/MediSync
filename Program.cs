using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "medisync_login.db");
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<PasswordValidatorService>();
builder.Services.AddSingleton<IdGenerationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseStaticFiles(); 

// 2. INICIALIZACIÓN DE DATOS (SEED)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated(); // Crea la BD si no existe

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        // Usuarios Base
        if (!context.Usuarios.Any(u => u.Rol == "Superusuario"))
        {
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = "99999", NombreCompleto = "Super Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                Rol = "Superusuario", FechaNacimiento = new DateTime(1980, 1, 1), Info = "Admin Sistema", Telefono = "0000-0000",
                Especialidad = "", FotoUrl = ""
            });
        }
        if (!context.Usuarios.Any(u => u.Rol == "Laboratorio"))
        {
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = "300000001", NombreCompleto = "Lic. Sarah Lab", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
                Rol = "Laboratorio", FechaNacimiento = new DateTime(1985, 5, 20), Info = "Jefa de Laboratorio", Telefono = "1111-1111",
                Especialidad = "", FotoUrl = ""
            });
        }
        if (!context.Usuarios.Any(u => u.Rol == "Doctor"))
        {
            context.Usuarios.AddRange(new List<Usuario>
            {
                new Usuario { IdUsuario="1000001", NombreCompleto="Dr. Gregory House", Rol="Doctor", PasswordHash=BCrypt.Net.BCrypt.HashPassword("pass"), Info="Diagnóstico.", Especialidad="Nefrología", FechaNacimiento=new DateTime(1959,5,15), Telefono="8888-1111", FotoUrl="" },
                new Usuario { IdUsuario="1000002", NombreCompleto="Dra. Meredith Grey", Rol="Doctor", PasswordHash=BCrypt.Net.BCrypt.HashPassword("pass"), Info="Cirugía General.", Especialidad="Cirugía General", FechaNacimiento=new DateTime(1978,1,1), Telefono="8888-2222", FotoUrl="" },
                new Usuario { IdUsuario="1000003", NombreCompleto="Dr. Shaun Murphy", Rol="Doctor", PasswordHash=BCrypt.Net.BCrypt.HashPassword("pass"), Info="Cirugía Pediátrica.", Especialidad="Cirugía Pediátrica", FechaNacimiento=new DateTime(1992,2,14), Telefono="8888-3333", FotoUrl="" }
            });
        }

        // Pacientes Base
        if (!context.Pacientes.Any())
        {
            context.Pacientes.AddRange(new List<Paciente>
            {
                new Paciente { NombreCompleto="Carlos Santana", IdLegal="001", Edad=65, Peso=78, Altura=1.75, Sexo="Masculino", Direccion="Centro", Telefono="2222-5555", HoraCita=DateTime.Now.Date.AddHours(9), EstadoCita="Pendiente", DoctorAsignado="Dr. Gregory House", HistoriaClinica="Hipertensión controlada." },
                new Paciente { NombreCompleto="Ana Gabriel", IdLegal="002", Edad=55, Peso=65, Altura=1.60, Sexo="Femenino", Direccion="Altamira", Telefono="8888-9999", HoraCita=DateTime.Now.AddHours(-1), EstadoCita="Pendiente", DoctorAsignado="Dra. Meredith Grey", HistoriaClinica="" }
            });
        }

        // Exámenes Base (CORREGIDO: ArchivoPdfUrl nunca es null)
        if (!context.Examenes.Any())
        {
            context.Examenes.AddRange(new List<Examen>
            {
                new Examen { PacienteNombre = "Carlos Santana", TipoExamen = "Hemograma Completo", Fecha = DateTime.Now.AddDays(-1), Estado = "Enviado", EsCritico = false, DatosResultado = "Hemoglobina: 14.5|Leucocitos: 7500", DoctorSolicitanteId = "1000001", NombreDoctor = "Dr. Gregory House", ArchivoPdfUrl = "resultado_hemograma_001.pdf" },
                // CORRECCION AQUI: Se agregó ArchivoPdfUrl = "" para evitar el crash
                new Examen { PacienteNombre = "Ana Gabriel", TipoExamen = "Perfil Lipídico", Fecha = DateTime.Now, Estado = "Pendiente", EsCritico = false, DatosResultado = "", DoctorSolicitanteId = "1000002", NombreDoctor = "Dra. Meredith Grey", ArchivoPdfUrl = "" }
            });
        }
        
        context.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR INICIALIZACION]: {ex.Message}");
    }
}

// 3. ENDPOINTS API

app.MapPost("/api/login", async (LoginRequest loginRequest, AppDbContext db) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == loginRequest.IdUsuario);
    if (usuario == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Contraseña, usuario.PasswordHash))
    {
        return Results.BadRequest(new { message = "Credenciales incorrectas" });
    }
    return Results.Ok(new LoginResponse { Token = $"token-{usuario.Rol}", Rol = usuario.Rol, NombreUsuario = usuario.NombreCompleto, IdUsuario = usuario.IdUsuario });
});

app.MapGet("/api/doctores", async (AppDbContext db) => Results.Ok(await db.Usuarios.Where(u => u.Rol == "Doctor").ToListAsync()));

app.MapGet("/api/usuarios/{idUsuario}", async (string idUsuario, AppDbContext db) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
    return usuario == null ? Results.NotFound() : Results.Ok(usuario);
});

app.MapPut("/api/usuarios/{idUsuario}", async (string idUsuario, UpdateProfileRequest request, AppDbContext db) =>
{
    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
    if (usuario == null) return Results.NotFound();
    usuario.Telefono = request.Telefono; usuario.Info = request.Info; usuario.FotoUrl = request.FotoUrl;
    await db.SaveChangesAsync();
    return Results.Ok(usuario);
});

app.MapPost("/api/usuarios/generar", async (CreateUserRequest newUser, AppDbContext db, IdGenerationService idGen, PasswordValidatorService passVal) =>
{
    var (passValida, passError) = passVal.Validar(newUser.Password);
    if (!passValida) return Results.BadRequest(new { message = passError });

    if (newUser.Rol != "Administrador" && newUser.Rol != "Doctor" && newUser.Rol != "Laboratorio")
        return Results.BadRequest(new { message = "Rol no permitido" });

    var nuevoId = idGen.GenerarId(newUser.Rol);
    var usuario = new Usuario
    {
        IdUsuario = nuevoId, NombreCompleto = newUser.NombreCompleto, PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.Password),
        Rol = newUser.Rol, FechaNacimiento = newUser.FechaNacimiento, Telefono = newUser.Telefono, Info = newUser.Info,
        Especialidad = newUser.Rol == "Doctor" ? newUser.Especialidad : "", FotoUrl = ""
    };
    db.Usuarios.Add(usuario);
    await db.SaveChangesAsync();
    return Results.Created($"/api/usuarios/{usuario.IdUsuario}", usuario);
});

app.MapGet("/api/pacientes", async (AppDbContext db) => 
{
    var pacientes = await db.Pacientes.ToListAsync();
    bool huboCambios = false;
    foreach (var p in pacientes) {
        if (p.EstadoCita == "Pendiente" && p.HoraCita != DateTime.MinValue && DateTime.Now > p.HoraCita.AddMinutes(15)) {
            p.EstadoCita = "Ausente"; huboCambios = true;
        }
    }
    if (huboCambios) await db.SaveChangesAsync();
    return Results.Ok(pacientes);
});

app.MapPost("/api/pacientes", async (Paciente np, AppDbContext db) =>
{
    // 1. Buscamos si ya existe un paciente con esa identificación (IdLegal)
    var pacienteExistente = await db.Pacientes.FirstOrDefaultAsync(p => p.IdLegal == np.IdLegal);

    if (pacienteExistente != null)
    {
        // 2. Si existe, SOLO ACTUALIZAMOS la cita y datos de contacto
        pacienteExistente.HoraCita = np.HoraCita;
        pacienteExistente.DoctorAsignado = np.DoctorAsignado;
        pacienteExistente.EstadoCita = "Pendiente";
        pacienteExistente.Telefono = np.Telefono; // Actualizamos tel por si cambió
        pacienteExistente.Direccion = np.Direccion;
        pacienteExistente.Peso = np.Peso;
        pacienteExistente.Altura = np.Altura;
        
        await db.SaveChangesAsync();
        return Results.Ok(pacienteExistente); // Retornamos el existente actualizado
    }
    else
    {
        // 3. Si NO existe, lo creamos como nuevo
        db.Pacientes.Add(np);
        await db.SaveChangesAsync();
        
        // Generamos IdLegal automático solo si no venía uno
        if (string.IsNullOrEmpty(np.IdLegal))
        {
            np.IdLegal = np.Id.ToString().PadLeft(3, '0');
            await db.SaveChangesAsync();
        }
        return Results.Created($"/api/pacientes/{np.Id}", np);
    }
});

app.MapPut("/api/pacientes/{id}", async (int id, Paciente p, AppDbContext db) =>
{
    var ex = await db.Pacientes.FindAsync(id);
    if (ex == null) return Results.NotFound();
    ex.NombreCompleto = p.NombreCompleto; ex.Edad = p.Edad; ex.Peso = p.Peso; ex.Altura = p.Altura;
    ex.Sexo = p.Sexo; ex.Direccion = p.Direccion; ex.Telefono = p.Telefono;
    ex.EstadoCita = p.EstadoCita; ex.DoctorAsignado = p.DoctorAsignado; 
    ex.HoraCita = p.HoraCita; ex.HistoriaClinica = p.HistoriaClinica;
    await db.SaveChangesAsync();
    return Results.Ok(ex);
});

app.MapGet("/api/examenes", async (AppDbContext db) => Results.Ok(await db.Examenes.OrderByDescending(e => e.Fecha).ToListAsync()));

app.MapPost("/api/examenes", async (Examen e, AppDbContext db) => 
{
    e.Fecha = DateTime.Now; 
    e.Estado = "Pendiente";
    // Evitamos error de base de datos si viene nulo
    if (e.ArchivoPdfUrl == null) e.ArchivoPdfUrl = ""; 
    
    db.Examenes.Add(e);
    await db.SaveChangesAsync();
    return Results.Created($"/api/examenes/{e.Id}", e);
});

app.MapPut("/api/examenes/{id}", async (int id, Examen e, AppDbContext db) => 
{
    var ex = await db.Examenes.FindAsync(id);
    if (ex == null) return Results.NotFound();
    ex.DatosResultado = e.DatosResultado; ex.Estado = e.Estado; 
    ex.EsCritico = e.EsCritico; ex.ArchivoPdfUrl = e.ArchivoPdfUrl;
    await db.SaveChangesAsync();
    return Results.Ok(ex);
});

app.MapPost("/api/examenes/{id}/upload", async (int id, HttpRequest request, AppDbContext db) =>
{
    var examen = await db.Examenes.FindAsync(id);
    if (examen == null) return Results.NotFound("Examen no encontrado");

    if (!request.HasFormContentType || request.Form.Files.Count == 0)
        return Results.BadRequest("No se envió ningún archivo");

    var file = request.Form.Files[0];
    var fileName = $"examen_{id}_{Guid.NewGuid()}.pdf";
    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

    using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    examen.ArchivoPdfUrl = fileName;
    await db.SaveChangesAsync();
    return Results.Ok(new { url = fileName });
});

app.Urls.Add("http://localhost:7151");
app.Run();

// 4. CLASES Y MODELOS

public class PasswordValidatorService
{
    public (bool, string) Validar(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return (false, "Mínimo 8 caracteres.");
        if (!Regex.IsMatch(password, @"[A-Z]")) return (false, "Falta 1 mayúscula.");
        if (!Regex.IsMatch(password, @"[0-9]")) return (false, "Falta 1 número.");
        return (true, "OK");
    }
}
public class IdGenerationService
{
    private readonly Random _random = new Random();
    public string GenerarId(string rol) => rol switch { 
        "Administrador" => _random.Next(10000, 99999).ToString(), 
        "Doctor" => _random.Next(1000000, 9999999).ToString(), 
        "Laboratorio" => _random.Next(100000000, 999999999).ToString(), 
        _ => _random.Next(10000, 99999).ToString() 
    };
}
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Paciente> Pacientes { get; set; }
    public DbSet<Examen> Examenes { get; set; }
}

public class Usuario { [Key] public int Id { get; set; } public string IdUsuario { get; set; } public string NombreCompleto { get; set; } public string PasswordHash { get; set; } public string Rol { get; set; } public DateTime FechaNacimiento { get; set; } public string Telefono { get; set; } public string Info { get; set; } public string Especialidad { get; set; } public string FotoUrl { get; set; } public int Edad => DateTime.Now.Year - FechaNacimiento.Year; }
public class Paciente { [Key] public int Id { get; set; } public string NombreCompleto { get; set; } public string IdLegal { get; set; } public int Edad { get; set; } public double Altura { get; set; } public double Peso { get; set; } public string Sexo { get; set; } public string Direccion { get; set; } public string Telefono { get; set; } public string EstadoCita { get; set; } = "Sin Cita"; public DateTime HoraCita { get; set; } public string DoctorAsignado { get; set; } public string HistoriaClinica { get; set; } }
public class Examen { [Key] public int Id { get; set; } public string PacienteNombre { get; set; } public string TipoExamen { get; set; } public DateTime Fecha { get; set; } public string Estado { get; set; } = "Pendiente"; public bool EsCritico { get; set; } = false; public bool EsUrgente { get; set; } = false; public string DatosResultado { get; set; } public string DoctorSolicitanteId { get; set; } public string NombreDoctor { get; set; } public string ArchivoPdfUrl { get; set; } }
public class LoginRequest { public string IdUsuario { get; set; } public string Contraseña { get; set; } }
public class LoginResponse { public string Token { get; set; } public string Rol { get; set; } public string NombreUsuario { get; set; } public string IdUsuario { get; set; } }
public class CreateUserRequest { public string NombreCompleto { get; set; } public string Password { get; set; } public string Rol { get; set; } public DateTime FechaNacimiento { get; set; } public string Telefono { get; set; } public string Info { get; set; } public string Especialidad { get; set; } }
public class UpdateProfileRequest { public string Telefono { get; set; } public string Info { get; set; } public string FotoUrl { get; set; } }
