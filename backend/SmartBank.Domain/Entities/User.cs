// SmartBank.Domain/Entities/User.cs
namespace SmartBank.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? AgencyId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public bool EmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        /// <summary>URL ou data URL de la photo de profil.</summary>
        public string? AvatarUrl { get; set; }
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? ProfessionalId { get; set; }
        public string? Gender { get; set; } // "Male" ou "Female"
        public string? PhoneNumber { get; set; }
        public string? AccountNumber { get; set; }

        // Navigation
        public Role Role { get; set; } = null!;
        public Agency? Agency { get; set; }
        public ICollection<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public string FullName => $"{FirstName} {LastName}";
    }
}
