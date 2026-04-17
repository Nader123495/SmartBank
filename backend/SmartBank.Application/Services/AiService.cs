using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartBank.Application.DTOs;
using SmartBank.Domain.Entities;
using SmartBank.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartBank.Application.Services
{
    public interface IAiService
    {
        Task<AiChatResponseDto?> ChatAsync(AiChatRequestDto request, int userId, string? ipAddress);
        Task<AiSuggestionDto?> ClassifyAsync(string title, string description, int userId, string? ipAddress);
        Task<string?> DraftResponseAsync(int complaintId, int userId, string? ipAddress);
        Task<string?> SummarizeAsync(int complaintId, int userId, string? ipAddress);
        Task<List<AiConversationHistoryDto>> GetHistoryAsync(int userId);
        Task<bool> CanMakeRequestAsync(int userId);
    }

    public class AiService : IAiService
    {
        private readonly SmartBankDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private const int DailyLimit = 50;
        private const string AuditAction = "AI_CHAT";

        /// <summary>Contexte et périmètre : l'assistant ne répond qu'aux questions sur SmartBank / réclamations bancaires.</summary>
        private static string GetChatSystemPrompt() =>
            @"Tu es l'assistant IA de SmartBank (STB Complaints), plateforme de gestion des réclamations bancaires.

## Périmètre strict — tu ne réponds QU'à ce qui concerne SmartBank / réclamations bancaires
- Sujets autorisés : la plateforme SmartBank, les réclamations bancaires, le dépôt/suivi de réclamations, le dashboard, les rôles (Agent, Responsable, Admin, Client), le workflow (statuts, SLA, escalade), l'utilisation de l'application (où cliquer, comment déposer une réclamation, etc.), l'architecture technique du projet (Angular, API .NET, base de données), les diagrammes ou explications sur ce projet uniquement.
- Hors sujet : tout ce qui ne concerne pas SmartBank ou les réclamations bancaires (actualités générales, recettes, autre logiciel, questions personnelles, etc.).

## Comportement obligatoire
- Si la question est hors du thème SmartBank / réclamations bancaires : réponds UNIQUEMENT en une courte phrase, de façon courtoise, que tu es l'assistant SmartBank et que tu ne peux répondre qu'aux questions sur la plateforme et les réclamations bancaires. Propose de reformuler une question sur le projet ou l'utilisation de l'application. Ne donne aucune information sur le sujet demandé.
- Si la question est dans le thème : réponds en français de façon précise et utile.

## Contexte technique du projet (pour les questions dans le thème)
- Frontend: Angular (standalone), TypeScript, SCSS. Routes: /dashboard, /complaints, /complaints/new, /admin/users, /admin/audit, /depot, /suivi.
- Backend: .NET 8, API REST. Entités: User, Complaint, ComplaintType, Agency, Role, AuditLog.
- Dashboard: KPI (réclamations, SLA, escalades), graphiques par type/statut/agence/priorité.
- Réclamations: référence, titre, description, type, canal, priorité (Faible/Moyenne/Haute/Critique), statut (Nouvelle, Assignée, En cours, Validation, Clôturée, Rejetée), SLA, commentaires, pièces jointes.
- Images jointes : si l'utilisateur envoie une capture d'écran ou une image du projet, analyse-la et réponds en restant sur le thème SmartBank.";

        public AiService(SmartBankDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<bool> CanMakeRequestAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            var count = await _context.AuditLogs
                .CountAsync(a => a.UserId == userId && a.Action == AuditAction && a.CreatedAt >= today);
            return count < DailyLimit;
        }

        public async Task<AiChatResponseDto?> ChatAsync(AiChatRequestDto request, int userId, string? ipAddress)
        {
            if (!await CanMakeRequestAsync(userId))
            {
                return new AiChatResponseDto
                {
                    Reply = "Limite quotidienne de requêtes IA atteinte (50/jour). Réessayez demain ou contactez un administrateur pour augmenter le quota."
                };
            }

            var userMessage = request.Message;
            var isPdf = !string.IsNullOrEmpty(request.AttachmentFileName) && (request.AttachmentMimeType ?? "").StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase);
            var isImage = !string.IsNullOrEmpty(request.AttachmentBase64) && (request.AttachmentMimeType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            if (isPdf)
                userMessage = $"[Document joint : {request.AttachmentFileName}. L'IA ne peut pas lire le PDF directement ; décrivez son contenu dans votre message si besoin.]\n\n" + userMessage;
            else if (isImage)
                userMessage = string.IsNullOrWhiteSpace(userMessage)
                    ? "Décris cette image et explique ce que tu vois (capture d'écran, interface, schéma, etc.)."
                    : "[Une image est jointe à ce message.] " + userMessage;

            var reply = await GenerateReplyAsync(
                userMessage,
                request.ConversationHistory,
                GetChatSystemPrompt(),
                request.AttachmentBase64,
                request.AttachmentMimeType,
                request.AttachmentFileName);
            if (string.IsNullOrWhiteSpace(reply))
            {
                return new AiChatResponseDto
                {
                    Reply = "Le service IA est temporairement indisponible. Vérifiez que le moteur IA est configuré (Ollama ou Anthropic) puis réessayez."
                };
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = AuditAction,
                CreatedAt = DateTime.UtcNow,
                Entity = "AI",
                UserId = userId,
                IPAddress = ipAddress
            });
            await _context.SaveChangesAsync();

            return new AiChatResponseDto { Reply = reply };
        }

        public async Task<AiSuggestionDto?> ClassifyAsync(string title, string description, int userId, string? ipAddress)
        {
            if (!await CanMakeRequestAsync(userId))
                return null;

            var prompt = $"Classifie cette réclamation. Titre: {title}. Description: {description}. Réponds en JSON avec: type (string), priority (Faible/Moyenne/Haute/Critique), suggestedAgent (string), confidence (0-1).";
            var reply = await GenerateReplyAsync(prompt, null, systemPrompt: null);
            if (string.IsNullOrWhiteSpace(reply))
                return new AiSuggestionDto { Type = "Autre", Priority = "Moyenne", SuggestedAgent = "", Confidence = 0.5f };

            try
            {
                var doc = JsonDocument.Parse(reply);
                var root = doc.RootElement;
                return new AiSuggestionDto
                {
                    Type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "Autre" : "Autre",
                    Priority = root.TryGetProperty("priority", out var p) ? p.GetString() ?? "Moyenne" : "Moyenne",
                    SuggestedAgent = root.TryGetProperty("suggestedAgent", out var a) ? a.GetString() ?? "" : "",
                    Confidence = root.TryGetProperty("confidence", out var c) ? (float)c.GetDouble() : 0.5f
                };
            }
            catch
            {
                return new AiSuggestionDto { Type = "Autre", Priority = "Moyenne", SuggestedAgent = "", Confidence = 0.5f };
            }
        }

        public async Task<string?> DraftResponseAsync(int complaintId, int userId, string? ipAddress)
        {
            if (!await CanMakeRequestAsync(userId))
                return null;

            var complaint = await _context.Complaints
                .Include(c => c.ComplaintType)
                .FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint == null)
                return null;

            var prompt = $"Rédige une réponse professionnelle pour la réclamation: {complaint.Title}. Description: {complaint.Description}. Ton courtois et orienté résolution.";
            return await GenerateReplyAsync(prompt, null, systemPrompt: null);
        }

        public async Task<string?> SummarizeAsync(int complaintId, int userId, string? ipAddress)
        {
            if (!await CanMakeRequestAsync(userId))
                return null;

            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint == null)
                return null;

            var prompt = $"Résume en 2-3 phrases: {complaint.Title}. {complaint.Description}";
            return await GenerateReplyAsync(prompt, null, systemPrompt: null);
        }

        public Task<List<AiConversationHistoryDto>> GetHistoryAsync(int userId)
        {
            return Task.FromResult(new List<AiConversationHistoryDto>());
        }

        private async Task<string?> GenerateReplyAsync(string message, List<AiMessageDto>? history, string? systemPrompt = null,
            string? attachmentBase64 = null, string? attachmentMimeType = null, string? attachmentFileName = null)
        {
            var provider = _config["AiProvider"] ?? "Ollama";
            var hasImage = !string.IsNullOrEmpty(attachmentBase64) && (attachmentMimeType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            return provider switch
            {
                "Anthropic" => await CallAnthropicAsync(message, history, systemPrompt, hasImage ? attachmentBase64 : null, attachmentMimeType),
                _ => await CallOllamaAsync(message, history, systemPrompt, hasImage ? attachmentBase64 : null, attachmentMimeType)
            };
        }

        private async Task<string?> CallOllamaAsync(string message, List<AiMessageDto>? history, string? systemPrompt = null, string? imageBase64 = null, string? imageMimeType = null)
        {
            var client = _httpClientFactory.CreateClient("Ollama");
            var model = _config["OllamaSettings:Model"] ?? "llama3.2";
            if (!string.IsNullOrEmpty(imageBase64))
            {
                // Vision: utiliser /api/chat avec un modèle vision (llava, llama3.2-vision, etc.)
                var visionModel = _config["OllamaSettings:VisionModel"] ?? "llava";
                var cleanBase64 = imageBase64.Replace("\r", "").Replace("\n", "").Trim();
                var contentList = new List<object> { new { type = "text", text = message } };
                contentList.Add(new { type = "image", image = cleanBase64 });
                var chatRequest = new
                {
                    model = visionModel,
                    messages = new[] { new { role = "user", content = contentList } },
                    stream = false
                };
                try
                {
                    var response = await client.PostAsJsonAsync("api/chat", chatRequest);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (json.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
                        return content.GetString();
                }
                catch { }
                return null;
            }
            object request;
            if (string.IsNullOrEmpty(systemPrompt))
                request = new { model, prompt = message, stream = false };
            else
                request = new { model, prompt = message, system = systemPrompt, stream = false };
            try
            {
                var response = await client.PostAsJsonAsync("api/generate", request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("response", out var resp))
                    return resp.GetString();
            }
            catch { }
            return null;
        }

        private async Task<string?> CallAnthropicAsync(string message, List<AiMessageDto>? history, string? systemPrompt = null, string? imageBase64 = null, string? imageMimeType = null)
        {
            var apiKey = _config["AnthropicSettings:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            var client = _httpClientFactory.CreateClient("Anthropic");

            if (!client.DefaultRequestHeaders.Contains("x-api-key"))
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
            if (!client.DefaultRequestHeaders.Contains("anthropic-version"))
            {
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }

            var model = _config["AnthropicSettings:Model"] ?? "claude-3-5-sonnet-20240620";
            var maxTokensConfig = _config["AnthropicSettings:MaxTokens"];
            var maxTokens = 1024;
            if (!string.IsNullOrWhiteSpace(maxTokensConfig) && int.TryParse(maxTokensConfig, out var parsed))
            {
                maxTokens = parsed;
            }

            object messageContent;
            if (!string.IsNullOrEmpty(imageBase64))
            {
                var mediaType = (imageMimeType ?? "image/png").Split(';')[0].Trim().ToLowerInvariant();
                if (mediaType != "image/jpeg" && mediaType != "image/png" && mediaType != "image/gif" && mediaType != "image/webp")
                    mediaType = "image/png";
                messageContent = new object[]
                {
                    new { type = "text", text = message },
                    new { type = "image", source = new { type = "base64", media_type = mediaType, data = imageBase64 } }
                };
            }
            else
            {
                messageContent = message;
            }

            object payload;
            if (string.IsNullOrEmpty(systemPrompt))
                payload = new { model, max_tokens = maxTokens, messages = new[] { new { role = "user", content = messageContent } } };
            else
                payload = new { model, max_tokens = maxTokens, system = systemPrompt, messages = new[] { new { role = "user", content = messageContent } } };

            try
            {
                var response = await client.PostAsJsonAsync("v1/messages", payload);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.Array &&
                    content.GetArrayLength() > 0)
                {
                    var first = content[0];
                    if (first.TryGetProperty("text", out var text))
                    {
                        return text.GetString();
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
