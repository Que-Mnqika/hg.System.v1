namespace HGTSWebApi.DTOs
{
    public class StudentAssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public Guid? RouteId { get; set; }
        public string? RouteName { get; set; }
        public string? RouteCode { get; set; }
        public string? LocationName { get; set; }
        public string? LocationAddress { get; set; }
        public string? PlacementType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
    }

    public class CreateStudentAssignmentDto
    {
        public Guid StudentId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? LocationAddress { get; set; }
        public string PlacementType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? RouteId { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateStudentAssignmentDto
    {
        public string? LocationName { get; set; }
        public string? LocationAddress { get; set; }
        public string? PlacementType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? RouteId { get; set; }
        public string? Status { get; set; }
    }
}