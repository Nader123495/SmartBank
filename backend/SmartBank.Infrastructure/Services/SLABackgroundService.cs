// SmartBank.Infrastructure/Services/SLABackgroundService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBank.Infrastructure.Data;

namespace SmartBank.Infrastructure.Services
{
    /// <summary>
    /// Background service that checks SLA violations every 15 minutes
    /// and auto-escalates overdue complaints.
    /// </summary>
    public class SLABackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SLABackgroundService> _logger;

        public SLABackgroundService(IServiceProvider services, ILogger<SLABackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAndEscalateAsync();
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task CheckAndEscalateAsync()
        {
            using var scope = _services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<SmartBankDbContext>();

            var overdueComplaints = await ctx.Complaints
                .Where(c => c.Status != "Clôturée" && c.Status != "Rejetée" &&
                    !c.IsEscalated &&
                    c.SLADeadline.HasValue &&
                    DateTime.UtcNow > c.SLADeadline)
                .ToListAsync();

            foreach (var complaint in overdueComplaints)
            {
                complaint.IsEscalated = true;
                complaint.EscalatedAt = DateTime.UtcNow;

                await ctx.SaveChangesAsync();

                try
                {
                    ctx.ComplaintStatusHistories.Add(new Domain.Entities.ComplaintStatusHistory
                    {
                        ComplaintId = complaint.Id,
                        OldStatus = complaint.Status,
                        NewStatus = complaint.Status,
                        ChangedAt = DateTime.UtcNow,
                        Comment = $"⚠️ Escalade automatique: SLA dépassé depuis {DateTime.UtcNow:dd/MM/yyyy HH:mm}"
                    });
                    await ctx.SaveChangesAsync();
                }
                catch { /* Table ComplaintStatusHistories absente */ }

                // Notify responsables
                var responsables = await ctx.Users
                    .Where(u => u.RoleId == 2 && u.IsActive)
                    .ToListAsync();

                foreach (var resp in responsables)
                {
                    ctx.Notifications.Add(new Domain.Entities.Notification
                    {
                        UserId = resp.Id,
                        Title = "🚨 SLA Dépassé - Escalade",
                        Message = $"La réclamation {complaint.Reference} a dépassé son délai SLA et a été escaladée.",
                        Type = "Alert",
                        ComplaintId = complaint.Id
                    });
                }

                _logger.LogWarning("Complaint {Ref} escalated due to SLA breach", complaint.Reference);
            }

            if (overdueComplaints.Any())
                await ctx.SaveChangesAsync();
        }
    }
}
