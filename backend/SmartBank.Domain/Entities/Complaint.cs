// SmartBank.Domain/Entities/Complaint.cs
namespace SmartBank.Domain.Entities
{
    public class Complaint
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ComplaintTypeId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Priority { get; set; } = "Moyenne";
        public string Status { get; set; } = "Nouvelle";
        public int? AgencyId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAccountNumber { get; set; }
        public string? ClientGovernorate { get; set; }
        // Geolocation at submission time
        public string? SubmissionCity { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? ClosedByUserId { get; set; }
        public string? ResolutionNote { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? SLADeadline { get; set; }
        public bool IsEscalated { get; set; } = false;
        public DateTime? EscalatedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        // Évaluation client (après clôture)
        public int? SatisfactionRating { get; set; }   // 1-5
        public int? SpeedRating { get; set; }           // 1-5
        public int? QualityRating { get; set; }         // 1-5
        public string? ClientFeedback { get; set; }

        // Specific fields for dynamic categories
        public string? CardLastFour { get; set; }
        public DateTime? IncidentDate { get; set; }
        public string? AccountType { get; set; }
        public decimal? Amount { get; set; }
        public string? VirementReference { get; set; }
        public string? CreditType { get; set; }
        public string? DossierNumber { get; set; }

        // Navigation
        public ComplaintType ComplaintType { get; set; } = null!;
        public Agency? Agency { get; set; }
        public User? AssignedTo { get; set; }
        public User? CreatedBy { get; set; }
        public ICollection<ComplaintStatusHistory> StatusHistory { get; set; } = new List<ComplaintStatusHistory>();
        public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public bool IsSLABreached => SLADeadline.HasValue && DateTime.UtcNow > SLADeadline;
        public bool IsClosed => Status is "Clôturée" or "Rejetée";
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Permissions { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    public class Agency
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Governorate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    public class ComplaintType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int DefaultSLAHours { get; set; } = 48;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ComplaintStatusHistory
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public int? ChangedByUserId { get; set; }
        public string? Comment { get; set; }
        /// <summary>Requis en base (NOT NULL). Valeur par défaut pour éviter les INSERT NULL.</summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public Complaint Complaint { get; set; } = null!;
        public User? ChangedBy { get; set; }
    }

    public class ComplaintAttachment
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? FileType { get; set; }
        public int? UploadedByUserId { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    public class Comment
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public Complaint Complaint { get; set; } = null!;
    }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public int? ComplaintId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public User User { get; set; } = null!;
    }

    public class AuditLog
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Entity { get; set; }
        public int? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SLAConfig
    {
        public int Id { get; set; }
        public int? ComplaintTypeId { get; set; }
        public string Priority { get; set; } = string.Empty;
        public int MaxHours { get; set; }
        public int EscalationHours { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
