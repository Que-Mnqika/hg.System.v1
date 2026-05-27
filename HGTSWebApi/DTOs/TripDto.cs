using System.ComponentModel.DataAnnotations;

namespace HGTSWebApi.DTOs
{
    // ============================================================================
    // READ DTO — returned by GET endpoints
    // ============================================================================
    public class TripDto
    {
        public Guid TripId { get; set; }
        public Guid RouteId { get; set; }
        public string? RouteName { get; set; }
        public string? RouteCode { get; set; }
        public Guid? InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public Guid DeviceId { get; set; }
        public string? DeviceIdentifier { get; set; }
        public Guid VehicleId { get; set; }
        public string? VehicleLabel { get; set; }

        // Scheduled window (set at creation, drives status transitions)
        public DateTime? ScheduledStartTime { get; set; }
        public DateTime? ScheduledEndTime { get; set; }

        // Actual times (set by TripStatusService when status changes)
        public DateTime? ActualStartTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // Assigned → InProgress → Completed (managed by TripStatusService)
        public string Status { get; set; } = string.Empty;

        public int BoardingCount { get; set; }
    }

    // ============================================================================
    // CREATE DTO — posted to POST /api/trips
    //
    // The caller provides a scheduled time window (e.g. 17:00 – 17:30).
    // Status is always set to "Assigned" by the controller — never from the client.
    // TripStatusService then transitions:
    //   Assigned  → InProgress  when ScheduledStartTime is reached
    //   InProgress→ Completed   when ScheduledEndTime is reached
    // ============================================================================
    public class CreateTripDto
    {
        [Required]
        public Guid RouteId { get; set; }

        [Required]
        public Guid DeviceId { get; set; }

        [Required]
        public Guid VehicleId { get; set; }

        /// <summary>
        /// When the trip is scheduled to begin — e.g. 2024-11-01T17:00:00Z
        /// The device will start accepting taps once TripStatusService flips
        /// status to InProgress at this time.
        /// </summary>
        [Required]
        public DateTime ScheduledStartTime { get; set; }

        /// <summary>
        /// When the trip is scheduled to end — e.g. 2024-11-01T17:30:00Z
        /// TripStatusService sets status to Completed at this time and the
        /// device stops accepting taps.
        /// </summary>
        [Required]
        public DateTime ScheduledEndTime { get; set; }
    }

    // ============================================================================
    // UPDATE DTO — posted to PATCH /api/trips/{id}
    // All fields optional — only provided fields are updated
    // ============================================================================
    public class UpdateTripDto
    {
        /// <summary>
        /// Reschedule the start time (only valid while status is still Assigned)
        /// </summary>
        public DateTime? ScheduledStartTime { get; set; }

        /// <summary>
        /// Reschedule the end time (only valid while status is Assigned or InProgress)
        /// </summary>
        public DateTime? ScheduledEndTime { get; set; }

        /// <summary>
        /// Manual status override — use sparingly; prefer letting TripStatusService
        /// handle transitions automatically.
        /// Valid values: Assigned | InProgress | Completed | Cancelled
        /// </summary>
        public string? Status { get; set; }

        // Legacy fields kept for compatibility with existing frontend calls
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}