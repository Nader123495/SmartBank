// SmartBank.Infrastructure/Data/SmartBankDbContext.cs
using Microsoft.EntityFrameworkCore;
using SmartBank.Domain.Entities;

namespace SmartBank.Infrastructure.Data
{
    public class SmartBankDbContext : DbContext
    {
        public SmartBankDbContext(DbContextOptions<SmartBankDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Agency> Agencies => Set<Agency>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintType> ComplaintTypes => Set<ComplaintType>();
        public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();
        public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SLAConfig> SLAConfigs => Set<SLAConfig>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
                e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
                e.HasOne(u => u.Agency).WithMany(a => a.Users).HasForeignKey(u => u.AgencyId);
            });

            // Complaint
            modelBuilder.Entity<Complaint>(e =>
            {
                e.HasIndex(c => c.Reference).IsUnique();
                e.HasOne(c => c.AssignedTo).WithMany(u => u.AssignedComplaints)
                    .HasForeignKey(c => c.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(c => c.CreatedBy).WithMany()
                    .HasForeignKey(c => c.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(c => c.ComplaintType).WithMany().HasForeignKey(c => c.ComplaintTypeId);
                e.HasOne(c => c.Agency).WithMany(a => a.Complaints).HasForeignKey(c => c.AgencyId);
                e.HasMany(c => c.Attachments).WithOne().HasForeignKey(a => a.ComplaintId);
            });

            // Types de réclamation (alignés avec le formulaire client)
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<ComplaintType>().HasData(
                new ComplaintType { Id = 1, Name = "Carte Bancaire", Code = "CARTE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                new ComplaintType { Id = 2, Name = "Crédit et Prêts", Code = "CREDIT", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                new ComplaintType { Id = 3, Name = "Compte Courant", Code = "COMPTE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                new ComplaintType { Id = 4, Name = "Digital Banking", Code = "DIGITAL", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                new ComplaintType { Id = 5, Name = "Virement", Code = "VIREMENT", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                new ComplaintType { Id = 6, Name = "Chèque", Code = "CHEQUE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate },
                new ComplaintType { Id = 7, Name = "Autre", Code = "AUTRE", DefaultSLAHours = 48, IsActive = true, CreatedAt = seedDate }
            );

            // StatusHistory — ChangedAt NOT NULL en base
            modelBuilder.Entity<ComplaintStatusHistory>(e =>
            {
                e.Property(s => s.ChangedAt).IsRequired();
                e.HasOne(s => s.Complaint).WithMany(c => c.StatusHistory)
                    .HasForeignKey(s => s.ComplaintId);
                e.HasOne(s => s.ChangedBy).WithMany()
                    .HasForeignKey(s => s.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
            });

            // Comment
            modelBuilder.Entity<Comment>(e =>
            {
                e.HasOne(c => c.User).WithMany()
                    .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(c => c.Complaint).WithMany(comp => comp.Comments)
                    .HasForeignKey(c => c.ComplaintId);
            });

            // Notification
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId);
            });
        }
    }
}
