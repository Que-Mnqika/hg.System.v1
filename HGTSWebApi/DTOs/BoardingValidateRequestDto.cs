using Azure;
using System;

namespace HGTSWebApi.DTOs
{
    // POST /boarding/validate
    public class BoardingValidateRequestDto
    {
        public string CredentialUid { get; set; } = string.Empty;  // Changed from CredentialToken
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string DeviceType { get; set; } = string.Empty;
        public string? TripId { get; set; }
        public string? RouteId { get; set; }
    }

    // Response for validation
    public class BoardingValidateResponseDto
    {
        public string? StudentId { get; set; }
        public bool AccessGranted { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ResultCode { get; set; } = string.Empty;
        public string? RouteId { get; set; }
        public string? ResidenceId { get; set; }
    }

    // POST /boarding/log
    public class BoardingLogRequestDto
    {
        public string Uid { get; set; } = string.Empty;
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool AccessGranted { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // POST /devices/{id}/telemetry
    public class TelemetryRequestDto
    {
        public int BatteryLevel { get; set; }
        public float Temperature { get; set; }
        public int Rssi { get; set; }
        public DateTime Timestamp { get; set; }
        public string? FirmwareVersion { get; set; }
    }

    // POST /devices/{id}/offline-sync/single
    public class OfflineTapDto
    {
        public string Uid { get; set; } = string.Empty;
        public string RawUid { get; set; } = string.Empty;
        public string TapTime { get; set; } = string.Empty;
        public bool AccessGranted { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? TripId { get; set; }
        public string? ResultCode { get; set; }
        public string? Message { get; set; }
    }

    // POST /devices/{id}/offline-sync/bulk
    public class OfflineSyncRequestDto
    {
        public List<OfflineTapDto> Taps { get; set; } = new();
    }

    public class OfflineSyncResponseDto
    {
        public int SyncedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Active Trip Response DTO
    public class ActiveTripResponseDto
    {
        public bool HasActiveTrip { get; set; }
        public string? TripId { get; set; }
        public string? RouteId { get; set; }
        public string? RouteName { get; set; }
        public string? ResidenceId { get; set; }
        public string? ResidenceName { get; set; }
        public int DurationHours { get; set; }
        public int DurationMinutes { get; set; }
        public int VehicleCapacity { get; set; }  // ADD THIS
        public DateTime? ScheduledEndTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Message { get; set; }
    }
}