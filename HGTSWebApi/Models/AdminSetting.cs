namespace HGTSApi.Models
{
    public class AdminSetting
    {
        public Guid AdminSettingId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? InstitutionId { get; set; }
        public string ScopeType { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string ValueType { get; set; } = "String";
        public string? Description { get; set; }
        public Guid UpdatedByUserId { get; set; }
        public string UpdatedByName { get; set; } = null!;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}