using System.Text;
using BackendApi.NHibernate;
using BackendApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// 👉 FIX: Avoid ISession conflict
using NHSession = NHibernate.ISession;
using ISessionFactory = NHibernate.ISessionFactory;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/backend-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

void LoadDotEnv()
{
    var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envFile))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith("#"))
            continue;

        var parts = trimmed.Split('=', 2);
        if (parts.Length != 2)
            continue;

        var key = parts[0].Trim();
        var value = parts[1].Trim();
        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
        {
            value = value[1..^1];
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}

// =====================
// SERILOG
// =====================
builder.Host.UseSerilog();

// =====================
// CORS
// =====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// =====================
// CONTROLLERS
// =====================
builder.Services.AddControllers();

// =====================
// SWAGGER
// =====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =====================
// NHIBERNATE
// =====================
builder.Services.AddSingleton(NHibernateHelper.SessionFactory);

// Session per request (FIXED)
builder.Services.AddScoped<NHSession>(sp =>
{
    var factory = sp.GetRequiredService<ISessionFactory>();
    return factory.OpenSession();
});

// =====================
// SERVICES
// =====================
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<LLMService>();
builder.Services.AddScoped<ChatFallbackService>();

// =====================
// JWT AUTH
// =====================
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing in appsettings.json");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });

var app = builder.Build();

// =====================
// MIDDLEWARE PIPELINE
// =====================
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =====================
// INIT LOG
// =====================
Log.Information("NHibernate SessionFactory initialized");

app.Run();