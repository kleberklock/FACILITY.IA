using Microsoft.EntityFrameworkCore;
using PROJFACILITY.IA.Data;
using PROJFACILITY.IA.Services;
using PROJFACILITY.IA.Models;
using PROJFACILITY.IA.Middlewares; // <--- NOVO
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BCrypt.Net;
using Serilog; // <--- NOVO

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAÇÃO DE LOGS (SERILOG) ---
// Isso cria arquivos de log diários na pasta /logs do projeto
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); 
// ------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// --- SEGURANÇA (JWT) ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new Exception("CRÍTICO: Configure Jwt:Key no appsettings.json");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; 
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500") // Aceita os dois formatos comuns
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Importante para tokens
    });
});

// --- BANCO DE DADOS ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// --- INJEÇÃO DE DEPENDÊNCIA ---
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<KnowledgeService>();

var app = builder.Build();

// --- ATIVAÇÃO DO ESCUDO DE ERROS (Deve vir logo no começo) ---
app.UseMiddleware<ExceptionHandlingMiddleware>();
// -------------------------------------------------------------

// --- INICIALIZAÇÃO DO BANCO ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        // Admin Padrão
        string emailAdmin = "admin@facility.ia";
        if (!context.Users.Any(u => u.Email == emailAdmin))
        {
            Log.Information("Criando conta Admin padrão...");
            context.Users.Add(new User
            {
                Name = "Administrador",
                Email = emailAdmin,
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"), 
                Role = "admin",
                Plan = "Enterprise",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
            Log.Information("Admin criado com sucesso.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro fatal na inicialização do banco.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("ProductionPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();