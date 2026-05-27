using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    public class StudentDto
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CellNumber { get; set; }
        public Guid InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public Guid? ResidenceId { get; set; }
        public string? ResidenceName { get; set; }
        public Guid? FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int CredentialCount { get; set; }
        public int PlacementCount { get; set; }
    }

    public class CreateStudentDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string StudentNumber { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string? CellNumber { get; set; }

        [Required]
        public Guid InstitutionId { get; set; }

        public Guid? ResidenceId { get; set; }  // Must be nullable!

        public Guid? FacultyId { get; set; }    // Must be nullable!

        public string? Status { get; set; }
    }

    public class UpdateStudentDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? CellNumber { get; set; }
        public Guid? ResidenceId { get; set; }
        public Guid? FacultyId { get; set; }
        public string? Status { get; set; }
    }
}