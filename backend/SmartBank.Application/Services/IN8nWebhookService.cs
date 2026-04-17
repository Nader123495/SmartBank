using SmartBank.Application.DTOs;

namespace SmartBank.Application.Services
{
    public interface IN8nWebhookService
    {
        /// <summary>Envoie le payload au Webhook n8n (HTTP POST). Sans effet si WebhookUrl est vide.</summary>
        Task NotifyComplaintCreatedAsync(ComplaintCreatedWebhookPayload payload, CancellationToken cancellationToken = default);
    }
}
