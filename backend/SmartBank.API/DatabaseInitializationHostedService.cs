using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartBank.Domain.Entities;
using SmartBank.Infrastructure.Data;

namespace SmartBank.API;

/// <summary>
/// Crée le catalogue / schéma et le seed hors du chemin synchrone avant Run(),
/// pour que Kestrel écoute tout de suite (évite les 502 Nginx pendant l’init SQL).
/// </summary>
public sealed class DatabaseInitializationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationHostedService> _logger;

    public DatabaseInitializationHostedService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<DatabaseInitializationHostedService> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunInitializationAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            // Ne jamais laisser remonter : en .NET 8 une exception ici peut arrêter l’hôte → Nginx 502.
            _logger.LogCritical(ex, "Initialisation base interrompue de façon inattendue.");
        }
    }

    private async Task RunInitializationAsync(CancellationToken stoppingToken)
    {
        var connectionString = SqlBootstrap.ResolveSmartBankConnectionString(_configuration);

        try
        {
            SqlBootstrap.EnsureSqlServerCatalogExists(connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossible de créer le catalogue SQL (tentatives seed à suivre).");
        }

        const int seedMaxAttempts = 25;
        for (var attempt = 1; attempt <= seedMaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SmartBankDbContext>();
                await db.Database.EnsureCreatedAsync(stoppingToken);

                await DefaultRolesSeeder.EnsureDefaultRolesAsync(db, stoppingToken);

                if (!await db.ComplaintTypes.AnyAsync(stoppingToken))
                {
                    var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    db.ComplaintTypes.AddRange(
                        new ComplaintType { Id = 1, Name = "Carte Bancaire", Code = "CARTE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                        new ComplaintType { Id = 2, Name = "Crédit et Prêts", Code = "CREDIT", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                        new ComplaintType { Id = 3, Name = "Compte Courant", Code = "COMPTE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                        new ComplaintType { Id = 4, Name = "Digital Banking", Code = "DIGITAL", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                        new ComplaintType { Id = 5, Name = "Virement", Code = "VIREMENT", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                        new ComplaintType { Id = 6, Name = "Chèque", Code = "CHEQUE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                        new ComplaintType { Id = 7, Name = "Autre", Code = "AUTRE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate });
                    await db.SaveChangesAsync(stoppingToken);
                }

                _logger.LogInformation("Base SmartBank initialisée (catalogue / schéma / types de réclamation).");
                return;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "Init SQL tentative {Attempt}/{Max}.", attempt, seedMaxAttempts);
                if (attempt < seedMaxAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Init DB tentative {Attempt}/{Max} (erreur non-SQL).", attempt, seedMaxAttempts);
                if (attempt < seedMaxAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _logger.LogCritical("Échec de l’initialisation de la base après {N} tentatives.", seedMaxAttempts);
    }
}
