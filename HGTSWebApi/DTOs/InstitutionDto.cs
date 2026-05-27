namespace HGTSWebApi.DTOs
{
    public class InstitutionDto
    {
        public Guid InstitutionId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public string InstitutionCode { get; set; } = string.Empty;
        public string? CampusName { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int FacultyCount { get; set; }
        public int ResidenceCount { get; set; }
        public int RouteCount { get; set; }
    }

    public class CreateInstitutionDto
    {
        public string InstitutionName { get; set; } = string.Empty;
        public string InstitutionCode { get; set; } = string.Empty;
        public string? CampusName { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateInstitutionDto
    {
        public string? InstitutionName { get; set; }
        public string? InstitutionCode { get; set; }
        public string? CampusName { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? Status { get; set; }
    }
}
