// ==========================================
// SLA DTOs - Data Transfer Objects
// ==========================================

namespace SmartBank.API.DTOs.SLA
{
    /// <summary>
    /// DTO pour créer un template SLA
    /// </summary>
    public class CreateSlaTemplateDto
    {
        public string Type { get; set; }
        public int SlaHours { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO pour mettre à jour un template SLA
    /// </summary>
    public class UpdateSlaTemplateDto
    {
        public string Type { get; set; }
        public int SlaHours { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO pour répondre avec un template SLA complet
    /// </summary>
    public class SlaTemplateDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int SlaHours { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public List<EscalationLevelDto> EscalationLevels { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO pour les niveaux d'escalade
    /// </summary>
    public class EscalationLevelDto
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public int MinutesThreshold { get; set; }
        public string TargetRole { get; set; }
        public List<string> Channels { get; set; } = new();
        public string RequiredAction { get; set; }
    }

    /// <summary>
    /// DTO pour ajouter un niveau d'escalade
    /// </summary>
    public class AddEscalationLevelDto
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public int MinutesThreshold { get; set; }
        public string TargetRole { get; set; }
        public List<string> Channels { get; set; } = new();
        public string RequiredAction { get; set; }
    }

    /// <summary>
    /// DTO pour les statistiques SLA
    /// </summary>
    public class SlaStatisticsDto
    {
        public int TotalComplaints { get; set; }
        public int MetSLA { get; set; }
        public int ApproachingSLA { get; set; }
        public int MissedSLA { get; set; }
        public double ComplianceRate { get; set; }
        public Dictionary<string, SlaTypeStatistics> ByType { get; set; } = new();
    }

    /// <summary>
    /// Statistiques par type de réclamation
    /// </summary>
    public class SlaTypeStatistics
    {
        public string Type { get; set; }
        public int Total { get; set; }
        public int Met { get; set; }
        public int Approaching { get; set; }
        public int Missed { get; set; }
        public double ComplianceRate { get; set; }
    }

    /// <summary>
    /// DTO pour vérifier la conformité SLA
    /// </summary>
    public class SlaComplianceDto
    {
        public int ComplaintId { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DueDate { get; set; }
        public int SlaHours { get; set; }
        public int HoursElapsed { get; set; }
        public int HoursRemaining { get; set; }
        public double PercentageElapsed { get; set; }
        public string ComplianceStatus { get; set; } // "Met", "Approaching", "Missed"
        public string EscalationLevel { get; set; }
    }

    /// <summary>
    /// DTO pour réponse d'erreur
    /// </summary>
    public class ApiErrorDto
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }

    /// <summary>
    /// DTO pour réponse de succès
    /// </summary>
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
    }
}
