namespace HGTSWebApi.DTOs
{
    public class CapacityNotificationDto
    {
        public Guid TripId { get; set; }
        public Guid RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public int BoardedCount { get; set; }
        public int Capacity { get; set; }
        public int RemainingCapacity { get; set; }
        public string CapacityBand { get; set; } = "GREEN";
        public string LastResultCode { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string? StudentName { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
