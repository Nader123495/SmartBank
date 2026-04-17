// SmartBank.API/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartBank.Application.Services;
using SmartBank.Infrastructure.Data;
using SmartBank.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// Database
// =============================================
builder.Services.AddDbContext<SmartBankDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("SmartBankDB")));

// =============================================
// Services (Dependency Injection)
// =============================================
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddHostedService<SLABackgroundService>();
builder.Services.AddHttpClient("Anthropic", c => c.BaseAddress = new Uri("https://api.anthropic.com/"));
var ollamaBaseUrl = builder.Configuration["OllamaSettings:BaseUrl"] ?? "http://localhost:11434";
builder.Services.AddHttpClient("Ollama", c => c.BaseAddress = new Uri(ollamaBaseUrl.TrimEnd('/') + "/"));

// =============================================
// JWT Authentication
// =============================================
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// =============================================
// CORS - Allow Angular dev server
// =============================================
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("SmartBankPolicy", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// =============================================
// Swagger
// =============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartBank Complaint Platform API",
        Version = "v1",
        Description = "STB Bank - Gestion des Réclamations"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// =============================================
// Seed des types de réclamation si table vide
// =============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartBankDbContext>();
    if (!db.ComplaintTypes.Any())
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.ComplaintTypes.AddRange(
            new SmartBank.Domain.Entities.ComplaintType { Id = 1, Name = "Carte Bancaire", Code = "CARTE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
            new SmartBank.Domain.Entities.ComplaintType { Id = 2, Name = "Crédit et Prêts", Code = "CREDIT", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
            new SmartBank.Domain.Entities.ComplaintType { Id = 3, Name = "Compte Courant", Code = "COMPTE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
            new SmartBank.Domain.Entities.ComplaintType { Id = 4, Name = "Digital Banking", Code = "DIGITAL", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
            new SmartBank.Domain.Entities.ComplaintType { Id = 5, Name = "Virement", Code = "VIREMENT", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
            new SmartBank.Domain.Entities.ComplaintType { Id = 6, Name = "Chèque", Code = "CHEQUE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
            new SmartBank.Domain.Entities.ComplaintType { Id = 7, Name = "Autre", Code = "AUTRE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate }
        );
        db.SaveChanges();
    }

    // Apply idempotent schema patches (new columns added outside EF migrations)
    try
    {
        var connStr = app.Configuration.GetConnectionString("SmartBankDB")!;
        SmartBank.API.SqlBootstrap.ApplySchemaPatches(connStr);
    }
    catch (Exception ex)
    {
        // Non-fatal: log and continue (columns may already exist or DB may be unreachable at cold start)
        app.Logger.LogWarning("Schema patches skipped: {Message}", ex.Message);
    }
}

// =============================================
// Middleware Pipeline
// =============================================
// Activer Swagger tout le temps (même hors Development)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("SmartBankPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
