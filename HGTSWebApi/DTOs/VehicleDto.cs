namespace HGTSWebApi.DTOs
{
    public class VehicleDto
    {
        public Guid VehicleId { get; set; }
        public string? VehicleName { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TripCount { get; set; }
        public int ActiveTripCount { get; set; }
        public int Capacity { get; set; }
    }

    public class CreateVehicleDto
    {
        public string? VehicleName { get; set; }
        public string? VehicleNumber { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateVehicleDto
    {
        public string? VehicleName { get; set; }
        public string? RegistrationNumber { get; set; }
        public int Capacity { get; set; }
        public string? Status { get; set; }
    }
}