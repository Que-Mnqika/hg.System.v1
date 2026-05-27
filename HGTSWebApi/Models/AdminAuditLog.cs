namespace HGTSApi.Models
{
    public class AdminAuditLog
    {
        public Guid AuditLogId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? InstitutionId { get; set; }
        public Guid ActorUserId { get; set; }
        public string ActorUsername { get; set; } = null!;
        public string ActorDisplayName { get; set; } = null!;
        public string ActorRole { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Summary { get; set; } = null!;
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}