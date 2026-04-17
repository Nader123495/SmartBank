// SmartBank.API/Controllers/AuthController.cs
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBank.Application.DTOs;
using SmartBank.Application.Services;
using SmartBank.Domain.Entities;
using SmartBank.Infrastructure.Data;
using System.Security.Claims;

namespace SmartBank.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IAuthService authService, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var recaptchaEnabled = string.Equals(_config["Recaptcha:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
            var recaptchaSecret = _config["Recaptcha:SecretKey"]?.Trim();
            if (recaptchaEnabled && !string.IsNullOrEmpty(recaptchaSecret))
            {
                if (string.IsNullOrWhiteSpace(dto.RecaptchaToken))
                    return BadRequest(new { message = "Vérification de sécurité requise. Cochez « Je ne suis pas un robot ».", code = "RECAPTCHA_REQUIRED" });
                var verifyOk = await VerifyRecaptchaAsync(dto.RecaptchaToken, ip, recaptchaSecret);
                if (!verifyOk)
                    return BadRequest(new { message = "Vérification de sécurité échouée. Réessayez.", code = "RECAPTCHA_FAILED" });
            }
            try
            {
                var result = await _authService.LoginAsync(dto, ip);
                if (result == null)
                    return Unauthorized(new { message = "Email ou mot de passe incorrect." });
                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_NOT_VERIFIED")
            {
                return StatusCode(403, new { message = "Compte non vérifié. Consultez votre boîte mail pour le code de vérification.", code = "EMAIL_NOT_VERIFIED", email = dto.Email });
            }
        }

        private async Task<bool> VerifyRecaptchaAsync(string token, string remoteIp, string secret)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var form = new Dictionary<string, string>
                {
                    ["secret"] = secret,
                    ["response"] = token,
                    ["remoteip"] = remoteIp
                };
                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(form));
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                return json.TryGetProperty("success", out var success) && success.GetBoolean();
            }
            catch
            {
                return false;
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await _authService.RegisterAsync(dto, ip);
                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_EXISTS")
            {
                return Conflict(new { message = "Cet email existe déjà." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto dto)
        {
            var result = await _authService.VerifyEmailAsync(dto.Email, dto.Code);
            if (result == null)
                return BadRequest(new { message = "Code invalide ou expiré." });
            return Ok(result);
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] VerifyEmailRequestDto dto)
        {
            var ok = await _authService.ResendVerificationEmailAsync(dto.Email);
            if (!ok)
                return BadRequest(new { message = "Impossible de renvoyer le code. Vérifiez l'adresse email ou le compte est déjà vérifié." });

            return Ok(new { message = "Un nouveau code de vérification a été généré. S'il n'arrive pas par email, contactez l'administrateur." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "L'adresse email est requise." });
            var result = await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Email, code et nouveau mot de passe sont requis." });
            if (dto.NewPassword.Length < 8)
                return BadRequest(new { message = "Le mot de passe doit contenir au moins 8 caractères." });
            var ok = await _authService.ResetPasswordAsync(dto.Email, dto.Code, dto.NewPassword);
            if (!ok)
                return BadRequest(new { message = "Code invalide ou expiré. Demandez un nouveau code." });
            return Ok(new { message = "Mot de passe mis à jour. Vous pouvez vous connecter." });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            if (result == null) return Unauthorized();
            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Déconnexion réussie." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _authService.UpdateProfileAsync(userId, dto);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Mot de passe actuel et nouveau requis." });
            if (dto.NewPassword.Length < 8)
                return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 8 caractères." });
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            if (!ok) return BadRequest(new { message = "Mot de passe actuel incorrect." });
            return Ok(new { message = "Mot de passe mis à jour." });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Name = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        /// <summary>Redirige vers la page de connexion Google (OAuth).</summary>
        [HttpGet("google")]
        public IActionResult Google()
        {
            var clientId = _config["GoogleOAuth:ClientId"];
            var redirectUri = _config["GoogleOAuth:BackendCallbackUrl"];
            var frontendUrl = _config["GoogleOAuth:FrontendCallbackUrl"] ?? "http://localhost:4200/auth/google-callback";
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                return Redirect(frontendUrl + "?error=" + Uri.EscapeDataString("Google OAuth non configuré. Renseignez GoogleOAuth:ClientId et BackendCallbackUrl dans appsettings.json."));
            var scope = Uri.EscapeDataString("openid email profile");
            var redirect = Uri.EscapeDataString(redirectUri);
            var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirect}&response_type=code&scope={scope}&access_type=offline&prompt=consent";
            return Redirect(url);
        }

        /// <summary>Callback Google : reçoit le code, échange contre un token, crée ou trouve l'utilisateur, redirige vers le frontend avec JWT.</summary>
        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? error)
        {
            var frontendUrl = _config["GoogleOAuth:FrontendCallbackUrl"] ?? "http://localhost:4200/auth/google-callback";
            if (!string.IsNullOrEmpty(error))
            {
                var errMsg = Uri.EscapeDataString(error == "access_denied" ? "Connexion Google annulée." : "Erreur Google : " + error);
                return Redirect($"{frontendUrl}?error={errMsg}");
            }
            if (string.IsNullOrEmpty(code))
            {
                return Redirect($"{frontendUrl}?error=" + Uri.EscapeDataString("Code d'autorisation absent."));
            }
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.GoogleLoginAsync(code, ip);
            if (result == null)
            {
                return Redirect($"{frontendUrl}?error=" + Uri.EscapeDataString("Échec de la connexion Google (vérifiez ClientId/ClientSecret et URI de redirection)."));
            }
            var token = Uri.EscapeDataString(result.AccessToken);
            var refresh = Uri.EscapeDataString(result.RefreshToken);
            var userJson = Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(result.User));
            return Redirect($"{frontendUrl}#token={token}&refresh={refresh}&user={userJson}");
        }
    }

    // ============================================================
    // Complaints Controller
    // ============================================================
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _service;
        private readonly ILogger<ComplaintsController> _logger;
        private readonly IWebHostEnvironment _env;
        public ComplaintsController(IComplaintService service, ILogger<ComplaintsController> logger, IWebHostEnvironment env)
        {
            _service = service;
            _logger = logger;
            _env = env;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ComplaintFilterDto filter)
        {
            // Agents can only see their assigned complaints
            if (User.IsInRole("Agent"))
                filter.AssignedToUserId = CurrentUserId;

            var result = await _service.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetById({Id}) failed", id);
                if (_env.IsDevelopment())
                    return StatusCode(500, new { message = "Erreur lors du chargement de la réclamation.", detail = ex.Message, stackTrace = ex.StackTrace?.Split('\n') });
                return StatusCode(500, new { message = "Erreur lors du chargement de la réclamation.", detail = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
        {
            var result = await _service.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateComplaintDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, CurrentUserId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Admin,Responsable")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignComplaintDto dto)
        {
            var result = await _service.AssignAsync(id, dto, CurrentUserId);
            if (!result) return NotFound();
            return Ok(new { message = "Réclamation assignée avec succès." });
        }

        [HttpPost("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NewStatus))
                return BadRequest(new { message = "Le nouveau statut est obligatoire." });
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var uid))
                    return Unauthorized(new { message = "Utilisateur non identifié." });
                var result = await _service.ChangeStatusAsync(id, dto, uid);
                if (!result) return NotFound();
                return Ok(new { message = "Statut mis à jour." });
            }
            catch (DbUpdateException ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Erreur base de données. Vérifiez que l'utilisateur existe (ex. admin Id=1). " + (string.IsNullOrEmpty(detail) ? "" : "Détail: " + detail), detail });
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentDto dto)
        {
            var result = await _service.AddCommentAsync(id, dto, CurrentUserId);
            if (!result) return NotFound();
            return Ok(new { message = "Commentaire ajouté." });
        }

        [HttpPost("{id}/auto-assign")]
        [Authorize(Roles = "Admin,Responsable")]
        public async Task<IActionResult> AutoAssign(int id)
        {
            var result = await _service.AutoAssignAsync(id);
            return result ? Ok(new { message = "Affectation automatique effectuée." }) : BadRequest();
        }
    }

    // ============================================================
    // Client Controller (portail public : dépôt et suivi sans auth)
    // ============================================================
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ClientController : ControllerBase
    {
        private readonly IComplaintService _service;

        public ClientController(IComplaintService service) => _service = service;

        /// <summary>Dépôt d'une réclamation par un client (sans compte).</summary>
        [HttpPost("depot")]
        public async Task<IActionResult> Depot([FromBody] CreateComplaintDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { message = "Titre et description sont obligatoires." });
            if (string.IsNullOrWhiteSpace(dto.ClientEmail))
                return BadRequest(new { message = "L'email du client est obligatoire pour le suivi." });
            try
            {
                var result = await _service.CreateForClientAsync(dto);
                return CreatedAtAction(nameof(Suivi), new { reference = result.Reference, email = result.ClientEmail }, new { reference = result.Reference, id = result.Id, message = "Réclamation enregistrée. Conservez la référence pour le suivi." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de l'enregistrement.", detail = ex.Message });
            }
        }

        /// <summary>Suivi d'une réclamation par référence + email.</summary>
        [HttpGet("suivi")]
        public async Task<IActionResult> Suivi([FromQuery] string reference, [FromQuery] string email)
        {
            try
            {
                var view = await _service.GetByReferenceAndEmailAsync(reference ?? "", email ?? "");
                if (view == null)
                    return NotFound(new { message = "Aucune réclamation trouvée pour cette référence et cet email." });
                return Ok(view);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du chargement du suivi.", detail = ex.Message });
            }
        }

        /// <summary>Ajouter un commentaire (client) sur une réclamation identifiée par référence + email.</summary>
        [HttpPost("suivi/comment")]
        public async Task<IActionResult> AddComment([FromBody] ClientCommentRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Reference) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Référence, email et contenu du commentaire sont obligatoires." });
            var ok = await _service.AddClientCommentAsync(dto.Reference, dto.Email, dto.Content);
            if (!ok) return NotFound(new { message = "Réclamation non trouvée ou impossible d'ajouter le commentaire." });
            return Ok(new { message = "Commentaire enregistré." });
        }
    }

    // ============================================================
    // Client Portal Controller (authentifié — portail client sécurisé)
    // ============================================================
    [ApiController]
    [Route("api/client-portal")]
    [Authorize(Roles = "Client")]
    public class ClientPortalController : ControllerBase
    {
        private readonly SmartBankDbContext _context;

        public ClientPortalController(SmartBankDbContext context) => _context = context;

        private string CurrentUserEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        /// <summary>
        /// Retourne UNIQUEMENT les réclamations déposées avec l'email du client connecté.
        /// Aucune donnée d'un autre client ne peut être vue — sécurisé par JWT.
        /// </summary>
        [HttpGet("my-complaints")]
        public async Task<IActionResult> MyComplaints()
        {
            var email = CurrentUserEmail.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "Impossible d'identifier votre compte." });

            var complaints = await _context.Complaints
                .AsNoTracking()
                .Include(c => c.ComplaintType)
                .Where(c => c.ClientEmail != null &&
                            c.ClientEmail.Trim().ToLower() == email)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClientComplaintSummaryDto
                {
                    Id = c.Id,
                    Reference = c.Reference,
                    Title = c.Title,
                    ComplaintType = c.ComplaintType != null ? c.ComplaintType.Name : "",
                    Status = c.Status,
                    Priority = c.Priority,
                    ClientGovernorate = c.ClientGovernorate,
                    HasRating = c.SatisfactionRating.HasValue,
                    CreatedAt = c.CreatedAt,
                    ClosedAt = c.ClosedAt
                })
                .ToListAsync();

            var total = complaints.Count;
            var active = complaints.Count(c => c.Status != "Clôturée" && c.Status != "Rejetée");
            var resolved = complaints.Count(c => c.Status == "Clôturée");

            return Ok(new
            {
                complaints,
                stats = new { total, active, resolved }
            });
        }

        /// <summary>
        /// Permet à un client d'évaluer une réclamation clôturée (1 seule fois par réclamation).
        /// </summary>
        [HttpPost("rate")]
        public async Task<IActionResult> Rate([FromBody] ClientRatingDto dto)
        {
            if (dto.SatisfactionRating < 1 || dto.SatisfactionRating > 5 ||
                dto.SpeedRating < 1 || dto.SpeedRating > 5 ||
                dto.QualityRating < 1 || dto.QualityRating > 5)
                return BadRequest(new { message = "Les notes doivent être entre 1 et 5." });

            var email = CurrentUserEmail.Trim().ToLowerInvariant();
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == dto.ComplaintId &&
                                          c.ClientEmail != null &&
                                          c.ClientEmail.Trim().ToLower() == email);

            if (complaint == null)
                return NotFound(new { message = "Réclamation non trouvée ou accès non autorisé." });

            if (complaint.SatisfactionRating.HasValue)
                return Conflict(new { message = "Vous avez déjà évalué cette réclamation." });

            if (complaint.Status != "Clôturée" && complaint.Status != "Rejetée")
                return BadRequest(new { message = "Vous ne pouvez évaluer qu'une réclamation clôturée ou rejetée." });

            complaint.SatisfactionRating = dto.SatisfactionRating;
            complaint.SpeedRating = dto.SpeedRating;
            complaint.QualityRating = dto.QualityRating;
            complaint.ClientFeedback = dto.Feedback?.Trim();
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Merci pour votre évaluation !" });
        }
    }

    // ============================================================
    // Dashboard Controller (UML : Responsable et Admin uniquement)
    // ============================================================
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Responsable")]
    public class DashboardController : ControllerBase
    {
        private readonly IComplaintService _service;
        public DashboardController(IComplaintService service) => _service = service;

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await _service.GetDashboardStatsAsync();
            return Ok(result);
        }
    }

    // ============================================================
    // Notifications Controller
    // ============================================================
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationsController(INotificationService service) => _service = service;

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetUserNotificationsAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var result = await _service.MarkAsReadAsync(id, CurrentUserId);
            return result ? Ok() : NotFound();
        }
    }

    // ============================================================
    // AI Assistant Controller
    // ============================================================
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService) => _aiService = aiService;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
        {
            var result = await _aiService.ChatAsync(request, CurrentUserId, IpAddress);
            return result != null ? Ok(result) : StatusCode(500, new { message = "Service IA indisponible." });
        }

        [HttpPost("classify")]
        public async Task<IActionResult> Classify([FromBody] AiClassifyRequestDto request)
        {
            if (!await _aiService.CanMakeRequestAsync(CurrentUserId))
                return StatusCode(429, new { message = "Limite quotidienne de requêtes IA atteinte (50/jour)." });
            var result = await _aiService.ClassifyAsync(request.Title, request.Description, CurrentUserId, IpAddress);
            return result != null ? Ok(result) : StatusCode(500, new { message = "Classification indisponible." });
        }

        [HttpPost("draft-response")]
        public async Task<IActionResult> DraftResponse([FromBody] AiDraftRequestDto body)
        {
            int complaintId = body.ComplaintId;
            if (!await _aiService.CanMakeRequestAsync(CurrentUserId))
                return StatusCode(429, new { message = "Limite quotidienne de requêtes IA atteinte (50/jour)." });
            var result = await _aiService.DraftResponseAsync(complaintId, CurrentUserId, IpAddress);
            return result != null ? Ok(new { draft = result }) : StatusCode(500, new { message = "Génération indisponible." });
        }

        [HttpPost("summarize/{id}")]
        public async Task<IActionResult> Summarize(int id)
        {
            if (!await _aiService.CanMakeRequestAsync(CurrentUserId))
                return StatusCode(429, new { message = "Limite quotidienne de requêtes IA atteinte (50/jour)." });
            var result = await _aiService.SummarizeAsync(id, CurrentUserId, IpAddress);
            return result != null ? Ok(new { summary = result }) : StatusCode(500, new { message = "Résumé indisponible." });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetHistory(int userId)
        {
            if (userId != CurrentUserId && !User.IsInRole("Admin"))
                return Forbid();
            var list = await _aiService.GetHistoryAsync(userId);
            return Ok(list);
        }

        [HttpGet("can-request")]
        public async Task<IActionResult> CanRequest()
        {
            var can = await _aiService.CanMakeRequestAsync(CurrentUserId);
            return Ok(new { can });
        }
    }

    // ============================================================
    // Users Controller (UML : Gérer les utilisateurs = Admin uniquement)
    // ============================================================
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly SmartBankDbContext _context;
        private readonly IAuthService _authService;
        private static readonly string[] ClosedStatuses = { "Clôturée", "Rejetée" };

        public UsersController(SmartBankDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var list = await _context.Roles.AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoleOptionDto { Id = r.Id, Name = r.Name })
                .ToListAsync();
            return Ok(list);
        }

        [AllowAnonymous]
        [HttpGet("agencies")]
        public async Task<IActionResult> GetAgencies()
        {
            var list = await _context.Agencies.AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .Select(a => new AgencyOptionDto { Id = a.Id, Name = a.Name })
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var activeUsers = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive)
                .GroupBy(u => u.Role != null ? u.Role.Name : "Inconnu")
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToListAsync();
            var totalActive = activeUsers.Sum(x => x.Count);
            var byRole = activeUsers.Select(x => new RoleCountDto { RoleName = x.RoleName, Count = x.Count }).ToList();
            return Ok(new UserStatsDto
            {
                TotalActive = totalActive,
                RolesCount = byRole.Count,
                ByRole = byRole
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? roleId,
            [FromQuery] int? agencyId)
        {
            var query = _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Agency)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.FirstName + " " + u.LastName + " " + u.Email).ToLower().Contains(term));
            }
            if (roleId.HasValue) query = query.Where(u => u.RoleId == roleId.Value);
            if (agencyId.HasValue) query = query.Where(u => u.AgencyId == agencyId.Value);

            var users = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();
            var countDict = new Dictionary<int, int>();
            if (userIds.Count > 0)
            {
                var openCounts = await _context.Complaints.AsNoTracking()
                    .Where(c => c.AssignedToUserId != null && userIds.Contains(c.AssignedToUserId.Value)
                        && !ClosedStatuses.Contains(c.Status))
                    .GroupBy(c => c.AssignedToUserId!.Value)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToListAsync();
                foreach (var x in openCounts) countDict[x.UserId] = x.Count;
            }

            var items = users.Select(u => new UserListItemDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role?.Name ?? "",
                AgencyId = u.AgencyId,
                AgencyName = u.Agency?.Name,
                IsActive = u.IsActive,
                EmailVerified = u.EmailVerified,
                LastLogin = u.LastLogin,
                ProfessionalId = u.ProfessionalId,
                Governorate = u.Governorate,
                OpenComplaintsCount = countDict.TryGetValue(u.Id, out var c) ? c : 0
            }).ToList();

            return Ok(items);
        }

        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = user.IsActive ? "ACTIVATE_USER" : "DEACTIVATE_USER",
                Entity = "User",
                EntityId = user.Id,
                NewValues = $"Utilisateur {user.Email} mis à jour (Actif: {user.IsActive})"
            });

            await _context.SaveChangesAsync();

            // Si l'admin active un compte Agent, on considère l'email comme vérifié
            // (l'admin fait office de validateur — l'agent peut se connecter immédiatement).
            if (user.IsActive && !user.EmailVerified && user.Role?.Name == "Agent")
            {
                user.EmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationExpiry = null;
                await _context.SaveChangesAsync();
            }

            return Ok(new { isActive = user.IsActive });
        }
    }

    // ============================================================
    // Audit Trail Controller (Admin uniquement)
    // ============================================================
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly SmartBankDbContext _context;

        public AuditController(SmartBankDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? action,
            [FromQuery] string? search,
            [FromQuery] int? userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            IQueryable<AuditLog> query = _context.AuditLogs.AsNoTracking()
                .OrderByDescending(a => a.CreatedAt);

            if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                var matchingUserIds = await _context.Users.AsNoTracking()
                    .Where(u => (u.FirstName + " " + u.LastName + " " + u.Email).ToLower().Contains(term))
                    .Select(u => u.Id).ToListAsync();
                query = query.Where(a =>
                    a.Action.ToLower().Contains(term) ||
                    (a.Entity != null && a.Entity.ToLower().Contains(term)) ||
                    (a.EntityId.HasValue && a.EntityId.ToString()!.Contains(term)) ||
                    (a.UserId.HasValue && matchingUserIds.Contains(a.UserId.Value)));
            }

            var total = await query.CountAsync();
            var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var logUserIds = logs.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value).Distinct().ToList();
            var userDict = new Dictionary<int, (string Name, string Email)>();
            if (logUserIds.Count > 0)
            {
                var userList = await _context.Users.AsNoTracking().Where(u => logUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName, u.Email }).ToListAsync();
                foreach (var u in userList) userDict[u.Id] = (u.Name, u.Email);
            }

            var items = logs.Select(a =>
            {
                var name = a.UserId.HasValue && userDict.TryGetValue(a.UserId.Value, out var uv) ? uv.Name : null;
                var email = a.UserId.HasValue && userDict.TryGetValue(a.UserId.Value, out var uv2) ? uv2.Email : null;
                var detail = BuildDetail(a.Action, a.Entity, a.EntityId, email);
                var entityLabel = a.EntityId.HasValue && !string.IsNullOrEmpty(a.Entity)
                    ? a.Entity + " #" + a.EntityId
                    : (a.Entity ?? (a.EntityId.HasValue ? "#" + a.EntityId : null));
                return new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = name,
                    UserEmail = email,
                    Action = a.Action,
                    Entity = entityLabel,
                    EntityId = a.EntityId,
                    Detail = detail,
                    IPAddress = string.IsNullOrEmpty(a.IPAddress) ? "système" : a.IPAddress,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();

            return Ok(new { items, totalCount = total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) });
        }

        private static string? BuildDetail(string action, string? entity, int? entityId, string? userEmail)
        {
            return action switch
            {
                "LOGIN" => !string.IsNullOrEmpty(userEmail) ? "Connexion réussie - " + userEmail : "Connexion réussie",
                "LOGOUT" => "Déconnexion",
                "REGISTER" => !string.IsNullOrEmpty(userEmail) ? "Inscription - " + userEmail : "Inscription",
                "CREATE" => entityId.HasValue ? "Réclamation #" + entityId + " créée" : "Création",
                "STATUS" => entityId.HasValue ? "Changement de statut - Réclamation #" + entityId : "Changement de statut",
                "ASSIGN" => entityId.HasValue ? "Assignation - Réclamation #" + entityId : "Assignation",
                "COMMENT" => entityId.HasValue ? "Commentaire ajouté - Réclamation #" + entityId : "Commentaire ajouté",
                "ESCALADE" or "ESCALATION" => entityId.HasValue ? "SLA dépassé - escalade automatique - Réclamation #" + entityId : "SLA dépassé - escalade automatique",
                _ => null
            };
        }
    }
}
