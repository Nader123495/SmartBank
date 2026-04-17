using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace SmartBank.Infrastructure.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string code, CancellationToken ct = default);
        Task SendAdminNotificationAsync(SmartBank.Domain.Entities.User agent, CancellationToken ct = default);
        Task<bool> SendPasswordResetEmailAsync(string email, string code, CancellationToken ct = default);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private string GetFromAddress(string? user, string? host)
        {
            var fromConfig = _config["Email:From"];
            if (!string.IsNullOrEmpty(fromConfig) && (string.IsNullOrEmpty(host) || !host.Contains("gmail")))
                return fromConfig;
            if (!string.IsNullOrEmpty(user))
                return $"SmartBank <{user}>";
            return "noreply@smartbank.tn";
        }

        public async Task SendVerificationEmailAsync(string email, string code, CancellationToken ct = default)
        {
            var host = _config["Email:SmtpHost"];
            var port = _config.GetValue<int>("Email:SmtpPort", 587);
            var user = _config["Email:UserName"];
            var password = _config["Email:Password"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Email non configuré. Code vérification pour {Email}: {Code} (appsettings.json → Email: UserName et Password requis)", email, code);
                return;
            }

            var from = GetFromAddress(user, host);
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "SmartBank — Vérification de votre compte";
            message.Body = new TextPart("html")
            {
                Text = $@"
<h2>Vérification de votre compte SmartBank</h2>
<p>Bonjour,</p>
<p>Votre code de vérification est : <strong>{code}</strong></p>
<p>Ce code est valide 15 minutes. Ne le partagez avec personne.</p>
<p>— L'équipe SmartBank STB</p>"
            };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
                await client.AuthenticateAsync(user, password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                _logger.LogInformation("Email de vérification envoyé à {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec envoi email vérification vers {Email}: {Message}", email, ex.Message);
                throw;
            }
        }

        public async Task SendAdminNotificationAsync(SmartBank.Domain.Entities.User agent, CancellationToken ct = default)
        {
            var adminEmail = _config["Email:AdminEmail"] ?? "admin@stb.tn";
            var host = _config["Email:SmtpHost"];
            var port = _config.GetValue<int>("Email:SmtpPort", 587);
            var user = _config["Email:UserName"];
            var password = _config["Email:Password"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Email non configuré pour la notification admin.");
                return;
            }

            var from = GetFromAddress(user, host);
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(adminEmail));
            message.Subject = "SmartBank — Nouvelle demande d'inscription Agent";
            message.Body = new TextPart("html")
            {
                Text = $@"
<h2>Nouvelle demande d'inscription Agent</h2>
<p>Un nouvel agent demande l'accès à la plateforme SmartBank :</p>
<ul>
    <li><strong>Nom complet :</strong> {agent.FullName}</li>
    <li><strong>E-mail :</strong> {agent.Email}</li>
    <li><strong>Matricule (Identifiant Pro) :</strong> {agent.ProfessionalId}</li>
    <li><strong>Gouvernorat :</strong> {agent.Governorate}</li>
    <li><strong>Date :</strong> {agent.CreatedAt:dd/MM/yyyy HH:mm}</li>
</ul>
<p>Veuillez vous connecter au tableau de bord administrateur pour valider ce compte.</p>
<p>— Système SmartBank STB</p>"
            };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
                await client.AuthenticateAsync(user, password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                _logger.LogInformation("Notification admin envoyée pour {Email}", agent.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec envoi notification admin pour {Email}: {Message}", agent.Email, ex.Message);
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string code, CancellationToken ct = default)
        {
            var host = _config["Email:SmtpHost"];
            var port = _config.GetValue<int>("Email:SmtpPort", 587);
            var user = _config["Email:UserName"];
            var password = _config["Email:Password"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Email non configuré. Code réinitialisation pour {Email}: {Code} (appsettings.json → Email: UserName et Password requis)", email, code);
                return false;
            }

            var from = GetFromAddress(user, host);
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "SmartBank — Réinitialisation de votre mot de passe";
            message.Body = new TextPart("html")
            {
                Text = $@"
<h2>Réinitialisation du mot de passe SmartBank</h2>
<p>Bonjour,</p>
<p>Votre code de réinitialisation est : <strong>{code}</strong></p>
<p>Ce code est valide 15 minutes. Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>
<p>— L'équipe SmartBank STB</p>"
            };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
                await client.AuthenticateAsync(user, password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                _logger.LogInformation("Email de réinitialisation mot de passe envoyé à {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec envoi email réinitialisation vers {Email}: {Message}", email, ex.Message);
                throw;
            }
        }
    }
}
