namespace SmartBank.Application.DTOs
{
    /// <summary>Payload JSON POST vers le Webhook n8n (événement complaint.created).</summary>
    public class ComplaintCreatedWebhookPayload
    {
        public string Event { get; set; } = "complaint.created";
        public int ComplaintId { get; set; }
        public string Reference { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public int ComplaintTypeId { get; set; }
        public string ComplaintTypeName { get; set; } = "";
        public string Channel { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public int? AgencyId { get; set; }
        public string? AgencyName { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public DateTime? SlaDeadlineUtc { get; set; }
        /// <summary>Délai SLA théorique en heures (config au moment de la création).</summary>
        public double? SlaMaxHours { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public bool IsEscalated { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        /// <summary>agent | client</summary>
        public string Source { get; set; } = "";
        /// <summary>URL de base de l’API pour les nœuds HTTP Request (ex. http://host.docker.internal:5000).</summary>
        public string? PublicApiUrl { get; set; }
    }

    /// <summary>Vue minimale pour les workflows n8n (GET sécurisé).</summary>
    public class ComplaintN8nSnapshotDto
    {
        public int ComplaintId { get; set; }
        public string Reference { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime? SlaDeadlineUtc { get; set; }
        public bool IsSlaBreached { get; set; }
        public bool IsEscalated { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class N8nNotifyRequestDto
    {
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Info";
    }
}
