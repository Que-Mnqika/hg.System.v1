
namespace HGTSWebApi.DTOs
{
    public class RouteStopDto
    {
        public Guid RouteStopId { get; set; }
        public Guid RouteId { get; set; }
        public string? RouteName { get; set; }
        public Guid ResidenceId { get; set; }
        public string? ResidenceName { get; set; }
        public int StopOrder { get; set; }
        //public int EstimatedTravelMinutesFromPrevious { get; set; }
        //public int DwellMinutes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateRouteStopDto
    {
        public Guid RouteId { get; set; }
        public Guid ResidenceId { get; set; }
        public int StopOrder { get; set; }
        //public int EstimatedTravelMinutesFromPrevious { get; set; } = 5; // default
        //public int DwellMinutes { get; set; } = 2;
    }

    public class UpdateRouteStopDto
    {
        public int? StopOrder { get; set; }
        public Guid? ResidenceId { get; set; }
        //public int? EstimatedTravelMinutesFromPrevious { get; set; }
        //public int? DwellMinutes { get; set; }
    }
}