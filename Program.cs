using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ClinicalAppointments.Api.Data; // <-- ¡DESCOMENTADO!
using ClinicalAppointments.Api.Services; // <-- Sigue comentado

var builder = WebApplication.CreateBuilder(args);
var Configuration = builder.Configuration;

// --- 1. Configuración de la Base de Datos (¡ACTUALIZADO!) ---
// Le decimos a C# que use el NUEVO DbContext (clinicalDbContext) y se conecte con SQLite.
builder.Services.AddDbContext<ClinicalContext>(options =>options.UseSqlite(Configuration.GetConnectionString("DefaultConnection"))); 

// --- 2. Servicios de Lógica de Negocio ---
 builder.Services.AddScoped<ICitaLogicService, CitaLogicService>(); // <-- Sigue comentado

// --- 3. Autenticación y Autorización (JWT) ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ... (código de JWT existente)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"]!,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]!))
        };
    });

// --- 4. Definición de Políticas de Roles ---
builder.Services.AddAuthorization(options =>
{
    // ¡OJO! Usamos los strings exactos de tu script SQL
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("superadmin", "personal_admin"));
    options.AddPolicy("DoctorOnly", policy => policy.RequireRole("doctor"));
    options.AddPolicy("LaboratorioOnly", policy => policy.RequireRole("laboratorio"));
    options.AddPolicy("AdminOrDoctor", policy => policy.RequireRole("superadmin", "personal_admin", "doctor"));
}); // <-- ¡BLOQUE DESCOMENTADO Y ADAPTADO!

// --- INICIO: Configuración de CORS ---
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy =>
                      {
                          // ¡¡CAMBIA ESTO por el puerto de tu Frontend!!
                          // He puesto los más comunes (React, Angular, Vue)
                          policy.WithOrigins("http://localhost:3000", // React
                                             "http://localhost:4200", // Angular
                                             "http://localhost:8080", // Vue
                                             "http://localhost:5173") // Vite
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});
// --- FIN: Configuración de CORS ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- INICIO: Habilitar CORS ---
// ¡Debe ir antes de UseAuthentication/UseAuthorization!
app.UseCors(myAllowSpecificOrigins);
// --- FIN: Habilitar CORS ---

// --- 5. Habilitar Autenticación y Autorización ---
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- 6. Tarea en segundo plano ---
// builder.Services.AddHostedService<CitaNoShowService>(); 

app.Run();