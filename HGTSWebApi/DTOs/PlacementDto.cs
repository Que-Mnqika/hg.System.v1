namespace HGTSWebApi.DTOs
{
    public class PlacementDto
    {
        public Guid PlacementId { get; set; }
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentNumber { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? LocationAddress { get; set; }
        public string PlacementType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? RouteId { get; set; }
        public string? RouteName { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreatePlacementDto
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

    public class UpdatePlacementDto
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