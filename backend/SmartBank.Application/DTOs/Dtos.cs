// SmartBank.Application/DTOs/AuthDTOs.cs
namespace SmartBank.Application.DTOs
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>Token reCAPTCHA v2 (case "Je ne suis pas un robot"). Requis si Recaptcha:SecretKey est configuré.</summary>
        public string? RecaptchaToken { get; set; }
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserProfileDto User { get; set; } = null!;
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Agency { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AccountNumber { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateProfileRequestDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AccountNumber { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RegisterRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        /// <summary>Ignoré : inscription publique = Client uniquement. Les Agents sont créés par l'admin.</summary>
        public int? RoleId { get; set; }
        public int? AgencyId { get; set; }
        public string? ProfessionalId { get; set; }
        public string? Governorate { get; set; }
        public string? Gender { get; set; }
        /// <summary>Ville de résidence (obligatoire à l'inscription).</summary>
        public string? City { get; set; }
        public bool IsAgentRequest { get; set; }
    }

    /// <summary>Réponse inscription : soit requiresVerification + email, soit tokens + user.</summary>
    public class RegisterResponseDto
    {
        public bool RequiresVerification { get; set; }
        public string? Email { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserProfileDto? User { get; set; }
    }

    public class VerifyEmailRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Réponse forgot-password. Le code n'est jamais renvoyé (sécurité) — l'utilisateur le reçoit par email et le saisit manuellement.</summary>
    public class ForgotPasswordResponseDto
    {
        public string Message { get; set; } = string.Empty;
    }

    // ============================================================
    // Complaint DTOs
    // ============================================================
    public class ComplaintListDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ComplaintType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? ClientGovernorate { get; set; }
        public string? AssignedTo { get; set; }
        public string? Agency { get; set; }
        public bool IsEscalated { get; set; }
        public bool IsSLABreached { get; set; }
        public DateTime? SLADeadline { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ComplaintDetailDto : ComplaintListDto
    {
        public string Description { get; set; } = string.Empty;
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAccountNumber { get; set; }
        public string? SubmissionCity { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ResolutionNote { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Specific fields
        public string? CardLastFour { get; set; }
        public DateTime? IncidentDate { get; set; }
        public string? AccountType { get; set; }
        public decimal? Amount { get; set; }
        public string? VirementReference { get; set; }
        public string? CreditType { get; set; }
        public string? DossierNumber { get; set; }

        public List<StatusHistoryDto> StatusHistory { get; set; } = new();
        public List<CommentDto> Comments { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class CreateComplaintDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ComplaintTypeId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Priority { get; set; } = "Moyenne";
        public int? AgencyId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAccountNumber { get; set; }
        public string? ClientGovernorate { get; set; }
        // Geolocation captured at submission time
        public string? SubmissionCity { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Specific fields
        public string? CardLastFour { get; set; }
        public DateTime? IncidentDate { get; set; }
        public string? AccountType { get; set; }
        public decimal? Amount { get; set; }
        public string? VirementReference { get; set; }
        public string? CreditType { get; set; }
        public string? DossierNumber { get; set; }
    }

    public class UpdateComplaintDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public string? Channel { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
    }

    public class AssignComplaintDto
    {
        public int AgentId { get; set; }
        public string? Notes { get; set; }
    }

    public class ChangeStatusDto
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public string? ResolutionNote { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class StatusHistoryDto
    {
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? ChangedBy { get; set; }
        public string? Comment { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddCommentDto
    {
        public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = true;
    }

    /// <summary>Vue limitée pour le client (suivi par référence + email).</summary>
    public class ClientComplaintViewDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ComplaintType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
        public string? RejectionReason { get; set; }
        public int? SatisfactionRating { get; set; }
        public int? SpeedRating { get; set; }
        public int? QualityRating { get; set; }
        public string? ClientFeedback { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public List<StatusHistoryDto> StatusHistory { get; set; } = new();
        public List<CommentDto> Comments { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    /// <summary>Vue résumée des réclamations pour le portail client authentifié.</summary>
    public class ClientComplaintSummaryDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ComplaintType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? ClientGovernorate { get; set; }
        public bool HasRating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    /// <summary>Évaluation client d'une réclamation clôturée.</summary>
    public class ClientRatingDto
    {
        public int ComplaintId { get; set; }
        public int SatisfactionRating { get; set; }  // 1-5
        public int SpeedRating { get; set; }          // 1-5
        public int QualityRating { get; set; }        // 1-5
        public string? Feedback { get; set; }
    }

    public class ClientCommentRequestDto
    {
        public string Reference { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? FileType { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // ============================================================
    // Dashboard DTOs
    // ============================================================
    public class DashboardStatsDto
    {
        public int TotalComplaints { get; set; }
        public int NewComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ClosedToday { get; set; }
        public int EscalatedComplaints { get; set; }
        public int SLABreachedComplaints { get; set; }
        public double AverageResolutionHours { get; set; }
        public double SLAComplianceRate { get; set; }
        public List<ComplaintsByTypeDto> ByType { get; set; } = new();
        public List<ComplaintsByStatusDto> ByStatus { get; set; } = new();
        public List<ComplaintsByAgencyDto> ByAgency { get; set; } = new();
        public List<ComplaintsByPriorityDto> ByPriority { get; set; } = new();
        public List<DailyTrendDto> DailyTrend { get; set; } = new();
        public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();
    }

    public class ComplaintsByTypeDto { public string Type { get; set; } = ""; public int Count { get; set; } }
    public class ComplaintsByStatusDto { public string Status { get; set; } = ""; public int Count { get; set; } }
    public class ComplaintsByAgencyDto { public string Agency { get; set; } = ""; public int Count { get; set; } }
    public class ComplaintsByPriorityDto { public string Priority { get; set; } = ""; public int Count { get; set; } }
    public class DailyTrendDto { public DateTime Date { get; set; } public int Created { get; set; } public int Closed { get; set; } }
    public class AgentPerformanceDto
    {
        public string AgentName { get; set; } = "";
        public int TotalAssigned { get; set; }
        public int TotalClosed { get; set; }
        public double AvgResolutionHours { get; set; }
        public double SLARate { get; set; }
    }

    // ============================================================
    // Pagination
    // ============================================================
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    public class ComplaintFilterDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? Channel { get; set; }
        public int? ComplaintTypeId { get; set; }
        public int? AgencyId { get; set; }
        public int? AssignedToUserId { get; set; }
        public bool? IsEscalated { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDir { get; set; } = "desc";
    }

    // ============================================================
    // User management DTOs
    // ============================================================
    public class UserListItemDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int? AgencyId { get; set; }
        public string? AgencyName { get; set; }
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public string? ProfessionalId { get; set; }
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? Gender { get; set; }
        public DateTime? LastLogin { get; set; }
        public int OpenComplaintsCount { get; set; }
    }

    public class UserStatsDto
    {
        public int TotalActive { get; set; }
        public int RolesCount { get; set; }
        public List<RoleCountDto> ByRole { get; set; } = new();
    }

    public class RoleCountDto
    {
        public string RoleName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RoleOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AgencyOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ============================================================
    // AI Assistant DTOs
    // ============================================================
    public class AiMessageDto
    {
        public string Role { get; set; } = "user"; // user | assistant
        public string Content { get; set; } = string.Empty;
    }

    public class AiChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public int? ComplaintId { get; set; }
        public List<AiMessageDto>? ConversationHistory { get; set; }
        /// <summary>Pièce jointe en base64 (image ou PDF) pour que l'IA aide à comprendre la plateforme.</summary>
        public string? AttachmentBase64 { get; set; }
        public string? AttachmentMimeType { get; set; }
        public string? AttachmentFileName { get; set; }
    }

    public class AiSuggestionDto
    {
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string SuggestedAgent { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

    public class AiChatResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public AiSuggestionDto? Suggestions { get; set; }
    }

    public class AiClassifyRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AiConversationHistoryDto
    {
        public long Id { get; set; }
        public int? ComplaintId { get; set; }
        public List<AiMessageDto> Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AiDraftRequestDto
    {
        public int ComplaintId { get; set; }
    }

    public class AuditLogDto
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Entity { get; set; }
        public int? EntityId { get; set; }
        public string? Detail { get; set; }
        public string? IPAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
