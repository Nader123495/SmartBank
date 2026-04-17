using Microsoft.EntityFrameworkCore;
using SmartBank.Domain.Entities;

namespace SmartBank.Infrastructure.Data;

/// <summary>
/// Rôles par défaut (Admin / Agent / Client). Idempotent — utilisé par l’init SQL en arrière-plan
/// et par l’inscription pour éviter une course où <c>Register</c> arrive avant la fin du seed.
/// </summary>
public static class DefaultRolesSeeder
{
    public static async Task EnsureDefaultRolesAsync(SmartBankDbContext db, CancellationToken cancellationToken = default)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var added = false;

        if (!await db.Roles.AnyAsync(r => r.Name == "Admin", cancellationToken))
        {
            db.Roles.Add(new Role
            {
                Name = "Admin",
                Description = "Administrateur — pilotage, assignation, utilisateurs, audit",
                Permissions = "[\"all\"]",
                CreatedAt = seedDate
            });
            added = true;
        }

        if (!await db.Roles.AnyAsync(r => r.Name == "Agent", cancellationToken))
        {
            db.Roles.Add(new Role
            {
                Name = "Agent",
                Description = "Agent de traitement des réclamations",
                Permissions = "[\"view_assigned\",\"create\",\"update\",\"comment\"]",
                CreatedAt = seedDate
            });
            added = true;
        }

        if (!await db.Roles.AnyAsync(r => r.Name == "Client", cancellationToken))
        {
            db.Roles.Add(new Role
            {
                Name = "Client",
                Description = "Client bancaire — dépôt et suivi des réclamations",
                Permissions = "[]",
                CreatedAt = seedDate
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync(cancellationToken);
    }
}
