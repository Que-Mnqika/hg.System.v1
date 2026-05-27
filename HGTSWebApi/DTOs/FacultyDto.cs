namespace HGTSWebApi.DTOs
{
    public class FacultyDto
    {
        public Guid FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public int StudentCount { get; set; }
    }

    public class CreateFacultyDto
    {
        public string FacultyName { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
    }

    public class UpdateFacultyDto
    {
        public string? FacultyName { get; set; }
        public Guid? InstitutionId { get; set; }
    }
}