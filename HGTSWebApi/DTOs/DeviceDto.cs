using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    public class DeviceDto
    {
        public Guid DeviceId { get; set; }
        public string DeviceIdentifier { get; set; } = string.Empty;
        public string? DeviceName { get; set; }

        public string? FirmwareVersion { get; set; }
        public string? HardwareVersion { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool IsOnline { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredDate { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public int? Rssi { get; set; }
        public int? BatteryLevel { get; set; }
        public int ActiveTripCount { get; set; }
        public int TotalBoardingCount { get; set; }

        // 🔥 NEW FIELD
        public int TripDurationHours { get; set; } = 6;
    }

    public class CreateDeviceDto
    {
        [Required]
        public string DeviceIdentifier { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? HardwareVersion { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }

        // 🔥 NEW FIELD (optional for create)
        public int? TripDurationHours { get; set; }
    }

    public class UpdateDeviceDto
    {
        public string? DeviceIdentifier { get; set; }
        public string? DeviceName { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? HardwareVersion { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }

        // 🔥 NEW FIELD for admin to update
        public int? TripDurationHours { get; set; }
    }

    // 🔥 NEW DTO for trip configuration
    public class TripConfigDto
    {
        public int TripDurationHours { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsOnline { get; set; }
        public string? DeviceName { get; set; }
    }

    // 🔥 NEW DTO for updating config from admin
    public class UpdateTripConfigDto
    {
        public int TripDurationHours { get; set; }
    }
}