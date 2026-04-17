using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBank.Application.DTOs;

namespace SmartBank.Application.Services
{
    public class N8nWebhookService : IN8nWebhookService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<N8nWebhookService> _logger;

        public N8nWebhookService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<N8nWebhookService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifyComplaintCreatedAsync(ComplaintCreatedWebhookPayload payload, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(_configuration["N8n:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
                return;

            var url = _configuration["N8n:WebhookUrl"]?.Trim();
            if (string.IsNullOrEmpty(url))
                return;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(
                int.TryParse(_configuration["N8n:WebhookTimeoutSeconds"], out var sec) ? sec : 15);

            var secret = _configuration["N8n:IntegrationSecret"];
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
            request.Headers.TryAddWithoutValidation("X-SmartBank-Event", payload.Event);
            if (!string.IsNullOrEmpty(secret))
                request.Headers.TryAddWithoutValidation("X-N8n-Secret", secret);

            try
            {
                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("n8n webhook HTTP {Status}: {Body}", (int)response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec appel webhook n8n (réclamation {Ref} — le flux applicatif continue).", payload.Reference);
            }
        }
    }
}
