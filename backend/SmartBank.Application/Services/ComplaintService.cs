// SmartBank.Application/Services/ComplaintService.cs
using Microsoft.EntityFrameworkCore;
using SmartBank.Application.DTOs;
using SmartBank.Domain.Entities;
using SmartBank.Infrastructure.Data;

namespace SmartBank.Application.Services
{
    public interface IComplaintService
    {
        Task<PagedResult<ComplaintListDto>> GetAllAsync(ComplaintFilterDto filter);
        Task<ComplaintDetailDto?> GetByIdAsync(int id);
        Task<ComplaintDetailDto> CreateAsync(CreateComplaintDto dto, int createdByUserId);
        /// <summary>Création par un client (sans compte) — CreatedByUserId = null.</summary>
        Task<ComplaintDetailDto> CreateForClientAsync(CreateComplaintDto dto);
        Task<ComplaintDetailDto?> UpdateAsync(int id, UpdateComplaintDto dto, int userId);
        Task<bool> AssignAsync(int id, AssignComplaintDto dto, int assignedByUserId);
        Task<bool> ChangeStatusAsync(int id, ChangeStatusDto dto, int userId);
        Task<bool> AddCommentAsync(int id, AddCommentDto dto, int userId);
        Task<ClientComplaintViewDto?> GetByReferenceAndEmailAsync(string reference, string email);
        Task<bool> AddClientCommentAsync(string reference, string email, string content);
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<bool> AutoAssignAsync(int complaintId);
    }

    public class ComplaintService : IComplaintService
    {
        private readonly SmartBankDbContext _context;
        private readonly INotificationService _notificationService;

        public ComplaintService(SmartBankDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<PagedResult<ComplaintListDto>> GetAllAsync(ComplaintFilterDto filter)
        {
            var query = _context.Complaints
                .Include(c => c.ComplaintType)
                .Include(c => c.Agency)
                .Include(c => c.AssignedTo)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(c => c.Reference.Contains(filter.Search) ||
                    c.Title.Contains(filter.Search) || c.ClientName!.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(c => c.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Priority))
                query = query.Where(c => c.Priority == filter.Priority);

            if (!string.IsNullOrEmpty(filter.Channel))
                query = query.Where(c => c.Channel == filter.Channel);

            if (filter.ComplaintTypeId.HasValue)
                query = query.Where(c => c.ComplaintTypeId == filter.ComplaintTypeId);

            if (filter.AgencyId.HasValue)
                query = query.Where(c => c.AgencyId == filter.AgencyId);

            if (filter.AssignedToUserId.HasValue)
                query = query.Where(c => c.AssignedToUserId == filter.AssignedToUserId);

            if (filter.IsEscalated.HasValue)
                query = query.Where(c => c.IsEscalated == filter.IsEscalated);

            if (filter.FromDate.HasValue)
                query = query.Where(c => c.CreatedAt >= filter.FromDate);

            if (filter.ToDate.HasValue)
                query = query.Where(c => c.CreatedAt <= filter.ToDate);

            // Sorting
            query = filter.SortBy switch
            {
                "Priority" => filter.SortDir == "asc" ? query.OrderBy(c => c.Priority) : query.OrderByDescending(c => c.Priority),
                "Status" => filter.SortDir == "asc" ? query.OrderBy(c => c.Status) : query.OrderByDescending(c => c.Status),
                "SLADeadline" => filter.SortDir == "asc" ? query.OrderBy(c => c.SLADeadline) : query.OrderByDescending(c => c.SLADeadline),
                _ => filter.SortDir == "asc" ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new ComplaintListDto
                {
                    Id = c.Id,
                    Reference = c.Reference,
                    Title = c.Title,
                    ComplaintType = c.ComplaintType.Name,
                    Channel = c.Channel,
                    Priority = c.Priority,
                    Status = c.Status,
                    ClientName = c.ClientName,
                    ClientGovernorate = c.ClientGovernorate,
                    AssignedTo = c.AssignedTo != null ? c.AssignedTo.FirstName + " " + c.AssignedTo.LastName : null,
                    Agency = c.Agency != null ? c.Agency.Name : null,
                    IsEscalated = c.IsEscalated,
                    IsSLABreached = c.SLADeadline.HasValue && DateTime.UtcNow > c.SLADeadline,
                    SLADeadline = c.SLADeadline,
                    CreatedAt = c.CreatedAt
                }).ToListAsync();

            return new PagedResult<ComplaintListDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ComplaintDetailDto?> GetByIdAsync(int id)
        {
            // Chargement en requêtes séparées pour éviter erreurs 500 (Include/ThenInclude complexes)
            var c = await _context.Complaints
                .AsNoTracking()
                .Include(x => x.ComplaintType)
                .Include(x => x.Agency)
                .Include(x => x.AssignedTo)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return null;

            List<ComplaintStatusHistory> statusHistory;
            try
            {
                statusHistory = await _context.ComplaintStatusHistories
                    .AsNoTracking()
                    .Where(s => s.ComplaintId == id)
                    .Include(s => s.ChangedBy)
                    .OrderBy(s => s.ChangedAt)
                    .ToListAsync();
            }
            catch
            {
                // Table ComplaintStatusHistories absente ou erreur SQL (ex. Invalid object name) :
                // on retourne un historique vide pour que la fiche détail s'affiche quand même.
                statusHistory = new List<ComplaintStatusHistory>();
            }

            List<Comment> comments;
            List<ComplaintAttachment> attachments;
            try
            {
                comments = await _context.Comments
                    .AsNoTracking()
                    .Where(cm => cm.ComplaintId == id)
                    .Include(cm => cm.User)
                    .OrderBy(cm => cm.CreatedAt)
                    .ToListAsync();
            }
            catch { comments = new List<Comment>(); }

            try
            {
                attachments = await _context.ComplaintAttachments
                    .AsNoTracking()
                    .Where(a => a.ComplaintId == id)
                    .ToListAsync();
            }
            catch { attachments = new List<ComplaintAttachment>(); }

            return new ComplaintDetailDto
            {
                Id = c.Id,
                Reference = c.Reference ?? "",
                Title = c.Title ?? "",
                Description = c.Description ?? "",
                ComplaintType = c.ComplaintType?.Name ?? "",
                Channel = c.Channel ?? "",
                Priority = c.Priority ?? "",
                Status = c.Status ?? "",
                ClientName = c.ClientName,
                ClientEmail = c.ClientEmail,
                ClientPhone = c.ClientPhone,
                ClientAccountNumber = c.ClientAccountNumber,
                AssignedTo = c.AssignedTo?.FullName,
                Agency = c.Agency?.Name,
                IsEscalated = c.IsEscalated,
                IsSLABreached = c.IsSLABreached,
                SLADeadline = c.SLADeadline,
                ResolutionNote = c.ResolutionNote,
                RejectionReason = c.RejectionReason,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ClosedAt = c.ClosedAt,
                StatusHistory = statusHistory.Select(s => new StatusHistoryDto
                {
                    OldStatus = s.OldStatus,
                    NewStatus = s.NewStatus ?? "",
                    ChangedBy = s.ChangedBy?.FullName,
                    Comment = s.Comment,
                    ChangedAt = s.ChangedAt
                }).ToList(),
                Comments = comments.Select(cm => new CommentDto
                {
                    Id = cm.Id,
                    Content = cm.Content ?? "",
                    AuthorName = cm.User?.FullName ?? "—",
                    IsInternal = cm.IsInternal,
                    CreatedAt = cm.CreatedAt
                }).ToList(),
                Attachments = attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName ?? "",
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                    UploadedAt = a.UploadedAt
                }).ToList(),
                CardLastFour = c.CardLastFour,
                IncidentDate = c.IncidentDate,
                AccountType = c.AccountType,
                Amount = c.Amount,
                VirementReference = c.VirementReference,
                CreditType = c.CreditType,
                DossierNumber = c.DossierNumber
            };
        }

        public async Task<ComplaintDetailDto> CreateAsync(CreateComplaintDto dto, int createdByUserId)
        {
            var reference = await GenerateReferenceAsync();

            // Get SLA config
            var slaConfig = await _context.SLAConfigs
                .FirstOrDefaultAsync(s => s.ComplaintTypeId == dto.ComplaintTypeId && s.Priority == dto.Priority && s.IsActive)
                ?? await _context.SLAConfigs.FirstOrDefaultAsync(s => s.Priority == dto.Priority && s.IsActive);

            var complaint = new Complaint
            {
                Reference = reference,
                Title = dto.Title,
                Description = dto.Description,
                ComplaintTypeId = dto.ComplaintTypeId,
                Channel = dto.Channel,
                Priority = dto.Priority,
                AgencyId = dto.AgencyId,
                ClientName = dto.ClientName,
                ClientEmail = dto.ClientEmail,
                ClientPhone = dto.ClientPhone,
                ClientAccountNumber = dto.ClientAccountNumber,
                SubmissionCity = dto.SubmissionCity,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CardLastFour = dto.CardLastFour,
                IncidentDate = dto.IncidentDate,
                AccountType = dto.AccountType,
                Amount = dto.Amount,
                VirementReference = dto.VirementReference,
                CreditType = dto.CreditType,
                DossierNumber = dto.DossierNumber,
                CreatedByUserId = createdByUserId,
                Status = "Nouvelle",
                SLADeadline = slaConfig != null ? DateTime.UtcNow.AddHours(slaConfig.MaxHours) : DateTime.UtcNow.AddHours(48)
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            try
            {
                _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                {
                    ComplaintId = complaint.Id,
                    NewStatus = "Nouvelle",
                    ChangedByUserId = createdByUserId,
                    ChangedAt = DateTime.UtcNow,
                    Comment = "Réclamation créée"
                });
                await _context.SaveChangesAsync();
            }
            catch { /* Table ComplaintStatusHistories absente */ }

            // Auto assign
            await AutoAssignAsync(complaint.Id);

            return (await GetByIdAsync(complaint.Id))!;
        }

        public async Task<ComplaintDetailDto> CreateForClientAsync(CreateComplaintDto dto)
        {
            var typeExists = await _context.ComplaintTypes.AnyAsync(t => t.Id == dto.ComplaintTypeId);
            if (!typeExists)
                throw new InvalidOperationException("Le type de réclamation sélectionné n'existe pas. Veuillez réessayer ou contacter le support.");

            var reference = await GenerateReferenceAsync();
            SLAConfig? slaConfig = null;
            try
            {
                slaConfig = await _context.SLAConfigs
                    .FirstOrDefaultAsync(s => s.ComplaintTypeId == dto.ComplaintTypeId && s.Priority == dto.Priority && s.IsActive)
                    ?? await _context.SLAConfigs.FirstOrDefaultAsync(s => s.Priority == dto.Priority && s.IsActive);
            }
            catch { /* Table SLAConfigs absente ou erreur : utiliser délai par défaut */ }

            var complaint = new Complaint
            {
                Reference = reference,
                Title = dto.Title,
                Description = dto.Description,
                ComplaintTypeId = dto.ComplaintTypeId,
                Channel = dto.Channel,
                Priority = dto.Priority,
                AgencyId = dto.AgencyId,
                ClientName = dto.ClientName,
                ClientEmail = dto.ClientEmail,
                ClientPhone = dto.ClientPhone,
                ClientAccountNumber = dto.ClientAccountNumber,
                ClientGovernorate = dto.ClientGovernorate,
                SubmissionCity = dto.SubmissionCity,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CardLastFour = dto.CardLastFour,
                IncidentDate = dto.IncidentDate,
                AccountType = dto.AccountType,
                Amount = dto.Amount,
                VirementReference = dto.VirementReference,
                CreditType = dto.CreditType,
                DossierNumber = dto.DossierNumber,
                CreatedByUserId = null,
                Status = "Nouvelle",
                SLADeadline = slaConfig != null ? DateTime.UtcNow.AddHours(slaConfig.MaxHours) : DateTime.UtcNow.AddHours(48)
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            try
            {
                _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                {
                    ComplaintId = complaint.Id,
                    NewStatus = "Nouvelle",
                    ChangedByUserId = null,
                    ChangedAt = DateTime.UtcNow,
                    Comment = "Réclamation déposée par le client"
                });
                await _context.SaveChangesAsync();
            }
            catch { /* Table absente */ }

            await AutoAssignAsync(complaint.Id);
            return (await GetByIdAsync(complaint.Id))!;
        }

        public async Task<ClientComplaintViewDto?> GetByReferenceAndEmailAsync(string reference, string email)
        {
            var refTrim = (reference ?? "").Trim();
            var emailTrim = (email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(refTrim) || string.IsNullOrEmpty(emailTrim)) return null;

            var c = await _context.Complaints
                .AsNoTracking()
                .Include(x => x.ComplaintType)
                .FirstOrDefaultAsync(x => x.Reference == refTrim && x.ClientEmail != null && x.ClientEmail.Trim().ToLower() == emailTrim);
            if (c == null) return null;

            List<ComplaintStatusHistory> statusHistory;
            try
            {
                statusHistory = await _context.ComplaintStatusHistories
                    .AsNoTracking()
                    .Where(s => s.ComplaintId == c.Id)
                    .Include(s => s.ChangedBy)
                    .OrderBy(s => s.ChangedAt)
                    .ToListAsync();
            }
            catch { statusHistory = new List<ComplaintStatusHistory>(); }

            List<Comment> comments;
            List<ComplaintAttachment> attachments;
            try
            {
                comments = await _context.Comments
                    .AsNoTracking()
                    .Where(cm => cm.ComplaintId == c.Id && !cm.IsInternal)
                    .Include(cm => cm.User)
                    .OrderBy(cm => cm.CreatedAt)
                    .ToListAsync();
            }
            catch { comments = new List<Comment>(); }

            try
            {
                attachments = await _context.ComplaintAttachments
                    .AsNoTracking()
                    .Where(a => a.ComplaintId == c.Id)
                    .ToListAsync();
            }
            catch { attachments = new List<ComplaintAttachment>(); }

            return new ClientComplaintViewDto
            {
                Id = c.Id,
                Reference = c.Reference ?? "",
                Title = c.Title ?? "",
                Description = c.Description ?? "",
                ComplaintType = c.ComplaintType?.Name ?? "",
                Channel = c.Channel ?? "",
                Priority = c.Priority ?? "",
                Status = c.Status ?? "",
                ResolutionNote = c.ResolutionNote,
                RejectionReason = c.RejectionReason,
                CreatedAt = c.CreatedAt,
                ClosedAt = c.ClosedAt,
                StatusHistory = statusHistory.Select(s => new StatusHistoryDto
                {
                    OldStatus = s.OldStatus,
                    NewStatus = s.NewStatus ?? "",
                    ChangedBy = s.ChangedBy?.FullName,
                    Comment = s.Comment,
                    ChangedAt = s.ChangedAt
                }).ToList(),
                Comments = comments.Select(cm => new CommentDto
                {
                    Id = cm.Id,
                    Content = cm.Content ?? "",
                    AuthorName = cm.User?.FullName ?? "—",
                    IsInternal = false,
                    CreatedAt = cm.CreatedAt
                }).ToList(),
                Attachments = attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName ?? "",
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };
        }

        public async Task<bool> AddClientCommentAsync(string reference, string email, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            var refTrim = (reference ?? "").Trim();
            var emailTrim = (email ?? "").Trim().ToLowerInvariant();
            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Reference == refTrim && c.ClientEmail != null && c.ClientEmail.Trim().ToLower() == emailTrim);
            if (complaint == null) return false;

            // Utilisateur "Client" pour les commentaires déposés sans compte (portail public)
            var clientUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Role.Name == "Client");
            var authorUserId = clientUser?.Id ?? await _context.Users.Select(u => u.Id).FirstOrDefaultAsync();
            if (authorUserId == 0) return false;

            _context.Comments.Add(new Comment
            {
                ComplaintId = complaint.Id,
                UserId = authorUserId,
                Content = content.Trim(),
                IsInternal = false
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AutoAssignAsync(int complaintId)
        {
            var complaint = await _context.Complaints
                .Include(c => c.ComplaintType)
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null) return false;

            // Étape 1 : chercher un agent actif du MÊME gouvernorat avec le moins de dossiers ouverts
            User? agent = null;
            if (!string.IsNullOrEmpty(complaint.ClientGovernorate))
            {
                agent = await _context.Users
                    .Where(u => u.RoleId == 2 && u.IsActive &&
                        u.Governorate != null &&
                        u.Governorate == complaint.ClientGovernorate)
                    .Select(u => new
                    {
                        User = u,
                        OpenCount = _context.Complaints.Count(c => c.AssignedToUserId == u.Id &&
                            c.Status != "Clôturée" && c.Status != "Rejetée")
                    })
                    .OrderBy(x => x.OpenCount)
                    .Select(x => x.User)
                    .FirstOrDefaultAsync();
            }

            // Étape 2 : fallback — n'importe quel agent actif
            if (agent == null)
            {
                agent = await _context.Users
                    .Where(u => u.RoleId == 2 && u.IsActive &&
                        (complaint.AgencyId == null || u.AgencyId == complaint.AgencyId))
                    .Select(u => new
                    {
                        User = u,
                        OpenCount = _context.Complaints.Count(c => c.AssignedToUserId == u.Id &&
                            c.Status != "Clôturée" && c.Status != "Rejetée")
                    })
                    .OrderBy(x => x.OpenCount)
                    .Select(x => x.User)
                    .FirstOrDefaultAsync();
            }

            if (agent == null) return false;

            complaint.AssignedToUserId = agent.Id;
            complaint.Status = "Assignée";

            await _context.SaveChangesAsync();

            try
            {
                _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                {
                    ComplaintId = complaint.Id,
                    OldStatus = "Nouvelle",
                    NewStatus = "Assignée",
                    ChangedAt = DateTime.UtcNow,
                    Comment = $"Affectation automatique à {agent.FullName}" +
                              (!string.IsNullOrEmpty(complaint.ClientGovernorate) && agent.Governorate == complaint.ClientGovernorate
                                  ? $" (même gouvernorat : {complaint.ClientGovernorate})"
                                  : " (fallback — aucun agent dans le gouvernorat du client)")
                });
                await _context.SaveChangesAsync();
            }
            catch { /* Table absente */ }

            try
            {
                await _notificationService.SendAsync(agent.Id,
                    "Nouvelle réclamation assignée",
                    $"La réclamation {complaint.Reference} vous a été assignée" +
                    (!string.IsNullOrEmpty(complaint.ClientGovernorate) ? $" (Gouvernorat : {complaint.ClientGovernorate})" : "") + ".",
                    "Info", complaint.Id);
            }
            catch { /* Ne pas faire échouer l'affectation si la notification échoue */ }

            return true;
        }

        public async Task<bool> ChangeStatusAsync(int id, ChangeStatusDto dto, int userId)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return false;

            var oldStatus = complaint.Status;
            complaint.Status = dto.NewStatus;
            complaint.UpdatedAt = DateTime.UtcNow;

            if (dto.NewStatus == "Clôturée")
            {
                complaint.ResolutionNote = dto.ResolutionNote;
                complaint.ClosedAt = DateTime.UtcNow;
                complaint.ClosedByUserId = userId;
            }
            else if (dto.NewStatus == "Rejetée")
            {
                complaint.RejectionReason = dto.RejectionReason;
                complaint.ClosedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            try
            {
                _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                {
                    ComplaintId = id,
                    OldStatus = oldStatus,
                    NewStatus = dto.NewStatus,
                    ChangedByUserId = userId,
                    ChangedAt = DateTime.UtcNow,
                    Comment = dto.Comment
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Table ComplaintStatusHistories absente : le statut a déjà été mis à jour ci-dessus.
            }

            return true;
        }

        public async Task<bool> AssignAsync(int id, AssignComplaintDto dto, int assignedByUserId)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return false;

            var oldAssignee = complaint.AssignedToUserId;
            complaint.AssignedToUserId = dto.AgentId;
            complaint.Status = "Assignée";
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            try
            {
                _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
                {
                    ComplaintId = id,
                    OldStatus = complaint.Status,
                    NewStatus = "Assignée",
                    ChangedByUserId = assignedByUserId,
                    ChangedAt = DateTime.UtcNow,
                    Comment = dto.Notes ?? "Réassignation manuelle"
                });
                await _context.SaveChangesAsync();
            }
            catch { /* Table absente */ }

            await _notificationService.SendAsync(dto.AgentId,
                "Réclamation assignée",
                $"La réclamation {complaint.Reference} vous a été assignée.",
                "Info", id);

            return true;
        }

        public async Task<ComplaintDetailDto?> UpdateAsync(int id, UpdateComplaintDto dto, int userId)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return null;

            if (dto.Title != null) complaint.Title = dto.Title;
            if (dto.Description != null) complaint.Description = dto.Description;
            if (dto.Priority != null) complaint.Priority = dto.Priority;
            if (dto.Channel != null) complaint.Channel = dto.Channel;
            if (dto.ClientName != null) complaint.ClientName = dto.ClientName;
            if (dto.ClientEmail != null) complaint.ClientEmail = dto.ClientEmail;
            if (dto.ClientPhone != null) complaint.ClientPhone = dto.ClientPhone;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> AddCommentAsync(int id, AddCommentDto dto, int userId)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return false;

            _context.Comments.Add(new Comment
            {
                ComplaintId = id,
                UserId = userId,
                Content = dto.Content,
                IsInternal = dto.IsInternal
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var all = _context.Complaints.AsQueryable();

            var stats = new DashboardStatsDto
            {
                TotalComplaints = await all.CountAsync(),
                NewComplaints = await all.CountAsync(c => c.Status == "Nouvelle"),
                InProgressComplaints = await all.CountAsync(c => c.Status == "En cours" || c.Status == "Assignée"),
                ClosedToday = await all.CountAsync(c => c.ClosedAt.HasValue && c.ClosedAt.Value.Date == today),
                EscalatedComplaints = await all.CountAsync(c => c.IsEscalated),
                SLABreachedComplaints = await all.CountAsync(c => c.SLADeadline.HasValue && DateTime.UtcNow > c.SLADeadline && c.Status != "Clôturée"),
                ByType = await all.GroupBy(c => c.ComplaintType.Name)
                    .Select(g => new ComplaintsByTypeDto { Type = g.Key, Count = g.Count() }).ToListAsync(),
                ByStatus = await all.GroupBy(c => c.Status)
                    .Select(g => new ComplaintsByStatusDto { Status = g.Key, Count = g.Count() }).ToListAsync(),
                ByAgency = await all.Where(c => c.Agency != null).GroupBy(c => c.Agency!.Name)
                    .Select(g => new ComplaintsByAgencyDto { Agency = g.Key, Count = g.Count() }).ToListAsync(),
                ByPriority = await all.GroupBy(c => c.Priority)
                    .Select(g => new ComplaintsByPriorityDto { Priority = g.Key, Count = g.Count() }).ToListAsync()
            };

            // Daily trend last 14 days
            var since = DateTime.UtcNow.AddDays(-14).Date;
            stats.DailyTrend = await all
                .Where(c => c.CreatedAt.Date >= since)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new DailyTrendDto
                {
                    Date = g.Key,
                    Created = g.Count(),
                    Closed = g.Count(c => c.Status == "Clôturée")
                }).OrderBy(d => d.Date).ToListAsync();

            return stats;
        }

        private async Task<string> GenerateReferenceAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _context.Complaints.CountAsync(c => c.CreatedAt.Year == year);
            return $"REC-{year}-{(count + 1):D6}";
        }
    }
}
