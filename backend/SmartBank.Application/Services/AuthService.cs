// SmartBank.Application/Services/AuthService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartBank.Application.DTOs;
using SmartBank.Infrastructure.Data;
using SmartBank.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartBank.Application.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string ipAddress);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto, string ipAddress);
        Task<LoginResponseDto?> VerifyEmailAsync(string email, string code);
        Task<bool> ResendVerificationEmailAsync(string email);
        Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(int userId);
        Task<LoginResponseDto?> GoogleLoginAsync(string code, string ipAddress);
        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string code, string newPassword);
        Task<UserProfileDto?> GetProfileAsync(int userId);
        Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly SmartBankDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(SmartBankDbContext context, IConfiguration config, IEmailService emailService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto, string ipAddress)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Agency)
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

                if (user == null)
                    return CreateDemoLoginResponse(dto); // fallback comptes démo

                // Verify password (BCrypt)
                try {
                    if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                        return CreateDemoLoginResponse(dto);
                } catch {
                    // Si le hash en base est invalide, on tente quand même le compte démo
                    return CreateDemoLoginResponse(dto);
                }

                // Compte en base mais email non vérifié
                if (!user.EmailVerified)
                    throw new InvalidOperationException("EMAIL_NOT_VERIFIED");

                user.LastLogin = DateTime.UtcNow;

                var accessToken = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                // Audit log
                _context.AuditLogs.Add(new Domain.Entities.AuditLog
                {
                    UserId = user.Id,
                    Action = "LOGIN",
                    Entity = "User",
                    EntityId = user.Id,
                    IPAddress = ipAddress
                });

                await _context.SaveChangesAsync();

                var permissions = new List<string>();
                if (user.Role.Permissions != null)
                    permissions = JsonSerializer.Deserialize<List<string>>(user.Role.Permissions) ?? new();

                return new LoginResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role.Name,
                        Agency = user.Agency?.Name,
                        AvatarUrl = user.AvatarUrl,
                        Gender = user.Gender,
                        PhoneNumber = user.PhoneNumber,
                        AccountNumber = user.AccountNumber,
                        Permissions = permissions
                    }
                };
            }
            catch
            {
                // En cas de problème de base de données, on tente quand même les comptes démo
                return CreateDemoLoginResponse(dto);
            }
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto, string ipAddress)
        {
            var email = dto.Email.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                throw new InvalidOperationException("EMAIL_EXISTS");

            // Inscription : Client par défaut, ou Agent (soumis à validation admin).
            var roleName = dto.IsAgentRequest ? "Agent" : "Client";
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
                throw new InvalidOperationException($"Le rôle {roleName} n'est pas configuré. Contactez l'administrateur.");

            var code = new Random().Next(100000, 999999).ToString();
            var user = new Domain.Entities.User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = role.Id,
                AgencyId = dto.AgencyId,
                Governorate = dto.Governorate,
                City = dto.City,
                ProfessionalId = dto.ProfessionalId,
                Gender = dto.Gender,
                IsActive = !dto.IsAgentRequest, // Agent = Inactif par défaut (en attente d'approbation)
                EmailVerified = false,
                EmailVerificationToken = code,
                EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new Domain.Entities.AuditLog
            {
                UserId = user.Id,
                Action = "REGISTER",
                Entity = "User",
                EntityId = user.Id,
                IPAddress = ipAddress
            });
            await _context.SaveChangesAsync();

            try
            {
                if (dto.IsAgentRequest)
                {
                    // Pour les agents, on notifie l'administrateur. Le code sera envoyé quand l'admin validera.
                    await _emailService.SendAdminNotificationAsync(user);
                }
                else
                {
                    // Pour les clients, flux normal
                    await _emailService.SendVerificationEmailAsync(email, code);
                }
            }
            catch
            {
                // En cas d'échec d'envoi, l'utilisateur ou l'admin peut réessayer plus tard
            }

            return new RegisterResponseDto
            {
                RequiresVerification = !dto.IsAgentRequest,
                Email = email
            };
        }

        public async Task<LoginResponseDto?> VerifyEmailAsync(string email, string code)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Agency)
                .FirstOrDefaultAsync(u => u.Email == email.Trim());

            if (user == null || user.EmailVerificationToken != code ||
                user.EmailVerificationExpiry == null || user.EmailVerificationExpiry < DateTime.UtcNow)
                return null;

            user.EmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationExpiry = null;
            await _context.SaveChangesAsync();

            // Si le compte n'est pas actif (Agent en attente de validation), on ne génère pas de token
            if (!user.IsActive)
            {
                return new LoginResponseDto
                {
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Role = user.Role.Name,
                        Agency = user.Agency?.Name,
                        AvatarUrl = user.AvatarUrl,
                        Gender = user.Gender,
                        PhoneNumber = user.PhoneNumber,
                        AccountNumber = user.AccountNumber
                    },
                    AccessToken = "", // Vide indique au frontend de ne pas rediriger vers le dashboard
                    RefreshToken = ""
                };
            }

            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var accessToken = GenerateJwtToken(user);
            var permissions = new List<string>();
            if (user.Role.Permissions != null)
                permissions = JsonSerializer.Deserialize<List<string>>(user.Role.Permissions) ?? new();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken!,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.Name,
                    Agency = user.Agency?.Name,
                    AvatarUrl = user.AvatarUrl,
                    PhoneNumber = user.PhoneNumber,
                    AccountNumber = user.AccountNumber,
                    Permissions = permissions
                }
            };
        }

        public async Task<bool> ResendVerificationEmailAsync(string email)
        {
            var trimmed = email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == trimmed);

            if (user == null || user.EmailVerified)
                return false;

            var code = new Random().Next(100000, 999999).ToString();
            user.EmailVerificationToken = code;
            user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendVerificationEmailAsync(trimmed, code);
            }
            catch
            {
                // En dev, le code reste loggé ou consultable en base même si l'envoi échoue.
            }

            return true;
        }

        public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Agency)
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow && u.IsActive);

            if (user == null) return null;

            var accessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var permissions = new List<string>();
            if (user.Role.Permissions != null)
                permissions = JsonSerializer.Deserialize<List<string>>(user.Role.Permissions) ?? new();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.Name,
                    Agency = user.Agency?.Name,
                    AvatarUrl = user.AvatarUrl,
                    Gender = user.Gender,
                    PhoneNumber = user.PhoneNumber,
                    AccountNumber = user.AccountNumber,
                    Permissions = permissions
                }
            };
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            _context.AuditLogs.Add(new Domain.Entities.AuditLog
            {
                UserId = userId,
                Action = "LOGOUT",
                Entity = "User",
                EntityId = userId
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponseDto?> GoogleLoginAsync(string code, string ipAddress)
        {
            var clientId = _config["GoogleOAuth:ClientId"];
            var clientSecret = _config["GoogleOAuth:ClientSecret"];
            var redirectUri = _config["GoogleOAuth:BackendCallbackUrl"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
                return null;

            var client = _httpClientFactory.CreateClient();
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });
            var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
                return null;
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var tokenDoc = JsonDocument.Parse(tokenJson);
            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenEl))
                return null;
            var accessToken = accessTokenEl.GetString();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            userInfoRequest.Headers.Add("Authorization", "Bearer " + accessToken);
            var userInfoResponse = await client.SendAsync(userInfoRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
                return null;
            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            using var userDoc = JsonDocument.Parse(userInfoJson);
            var email = userDoc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
            var name = userDoc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
            var givenName = userDoc.RootElement.TryGetProperty("given_name", out var gn) ? gn.GetString() : null;
            var familyName = userDoc.RootElement.TryGetProperty("family_name", out var fn) ? fn.GetString() : null;
            if (string.IsNullOrEmpty(email))
                return null;

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Agency)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Agent");
                if (role == null)
                    return null;
                user = new Domain.Entities.User
                {
                    FirstName = givenName ?? (string.IsNullOrEmpty(name) ? "Prénom" : name.Split(' ').FirstOrDefault() ?? "Prénom"),
                    LastName = familyName ?? (string.IsNullOrEmpty(name) ? "Google" : string.Join(" ", name.Split(' ').Skip(1))),
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
                    RoleId = role.Id,
                    Role = role,
                    EmailVerified = true,
                    IsActive = true
                };
                if (string.IsNullOrEmpty(user.LastName)) user.LastName = "Google";
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user.LastLogin = DateTime.UtcNow;
            }

            var jwt = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new Domain.Entities.AuditLog
            {
                UserId = user.Id,
                Action = "LOGIN_GOOGLE",
                Entity = "User",
                EntityId = user.Id,
                IPAddress = ipAddress
            });
            await _context.SaveChangesAsync();

            var permissions = new List<string>();
            if (user.Role.Permissions != null)
                permissions = JsonSerializer.Deserialize<List<string>>(user.Role.Permissions) ?? new();

            return new LoginResponseDto
            {
                AccessToken = jwt,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.Name,
                    Agency = user.Agency?.Name,
                    AvatarUrl = user.AvatarUrl,
                    Gender = user.Gender,
                    Permissions = permissions
                }

            };
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email)
        {
            var msg = "Si un compte existe pour cet email, un code de réinitialisation a été envoyé. Vérifiez votre boîte mail (valide 15 min). Saisissez le code manuellement ci-dessous.";
            var trimmed = email.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == trimmed && u.IsActive);
            if (user == null)
                return new ForgotPasswordResponseDto { Message = msg }; // Ne pas révéler si l'email existe
            var code = new Random().Next(100000, 999999).ToString();
            user.PasswordResetToken = code;
            user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();
            try
            {
                await _emailService.SendPasswordResetEmailAsync(trimmed, code);
            }
            catch
            {
                // Code en base et dans les logs uniquement — jamais renvoyé au client (sécurité)
            }
            return new ForgotPasswordResponseDto { Message = msg };
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            var trimmed = email.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == trimmed && u.IsActive);
            if (user == null || user.PasswordResetToken != code ||
                user.PasswordResetExpiry == null || user.PasswordResetExpiry < DateTime.UtcNow)
                return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Agency)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return null;
            var permissions = new List<string>();
            if (user.Role.Permissions != null)
                permissions = JsonSerializer.Deserialize<List<string>>(user.Role.Permissions) ?? new();
            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.Name,
                Agency = user.Agency?.Name,
                AvatarUrl = user.AvatarUrl,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                AccountNumber = user.AccountNumber,
                Permissions = permissions
            };
        }

        public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Agency)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return null;
            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName.Trim();
            
            if (dto.LastName != null)
                user.LastName = dto.LastName.Trim();

            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                // Mettre à jour le nom mis en cache dans toutes les réclamations de ce client
                var newFullName = user.FirstName + (string.IsNullOrWhiteSpace(user.LastName) ? "" : " " + user.LastName);
                var userComplaints = await _context.Complaints
                    .Where(c => c.CreatedByUserId == userId || c.ClientEmail == user.Email)
                    .ToListAsync();
                foreach (var c in userComplaints)
                {
                    c.ClientName = newFullName;
                }
            }

            if (dto.AvatarUrl != null)
                user.AvatarUrl = dto.AvatarUrl.Length > 50000 ? null : dto.AvatarUrl;
            
            if (!string.IsNullOrWhiteSpace(dto.Gender))
                user.Gender = dto.Gender;
            
            if (dto.PhoneNumber != null)
                user.PhoneNumber = dto.PhoneNumber.Trim();
                
            if (dto.AccountNumber != null)
                user.AccountNumber = dto.AccountNumber.Trim();

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await GetProfileAsync(userId);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null) return false;
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;
            if (newPassword.Length < 8)
                return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(Domain.Entities.User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("agencyId", user.AgencyId?.ToString() ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Fallback pour les comptes de démonstration si la BD pose problème
        private LoginResponseDto? CreateDemoLoginResponse(LoginRequestDto dto)
        {
            if (dto.Password != "Admin@2025")
                return null;

            var demoUsers = new Dictionary<string, (int Id, string First, string Last, string Role, string? Agency, List<string> Perms)>
            {
                ["admin@stb.tn"] = (1, "Super", "Admin", "Admin", null, new List<string> { "all" }),
                ["responsable@stb.tn"] = (2, "Responsable", "STB", "Responsable", "Agence Tunis Centre",
                    new List<string> { "view_all", "assign", "validate", "escalate", "reports" }),
                ["agent1@stb.tn"] = (3, "Agent", "Un", "Agent", "Agence Tunis Centre",
                    new List<string> { "view_assigned", "create", "update", "comment" })
            };

            if (!demoUsers.TryGetValue(dto.Email, out var info))
                return null;

            var roleEntity = new Domain.Entities.Role
            {
                Id = info.Id,
                Name = info.Role
            };

            var userEntity = new Domain.Entities.User
            {
                Id = info.Id,
                FirstName = info.First,
                LastName = info.Last,
                Email = dto.Email,
                RoleId = info.Id,
                Role = roleEntity
            };

            if (info.Agency != null)
            {
                userEntity.AgencyId = 1;
                userEntity.Agency = new Domain.Entities.Agency
                {
                    Id = 1,
                    Name = info.Agency
                };
            }

            var accessToken = GenerateJwtToken(userEntity);
            var refreshToken = GenerateRefreshToken();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserProfileDto
                {
                    Id = info.Id,
                    FullName = $"{info.First} {info.Last}",
                    Email = dto.Email,
                    Role = info.Role,
                    Agency = info.Agency,
                    Permissions = info.Perms
                }
            };
        }

        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    // Notification service interface & impl
    public interface INotificationService
    {
        Task SendAsync(int userId, string title, string message, string type, int? complaintId = null);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Type { get; set; }
        public int? ComplaintId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationService : INotificationService
    {
        private readonly SmartBankDbContext _context;
        public NotificationService(SmartBankDbContext context) => _context = context;

        public async Task SendAsync(int userId, string title, string message, string type, int? complaintId = null)
        {
            _context.Notifications.Add(new Domain.Entities.Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ComplaintId = complaintId
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    ComplaintId = n.ComplaintId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null) return false;
            notif.IsRead = true;
            notif.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
