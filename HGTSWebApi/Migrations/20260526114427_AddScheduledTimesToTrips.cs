using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledTimesToTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Buses",
                schema: "dbo",
                columns: table => new
                {
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buses", x => x.BusId);
                });

            migrationBuilder.CreateTable(
                name: "DashboardUsers",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardUsers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Institutions",
                schema: "dbo",
                columns: table => new
                {
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CampusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.InstitutionId);
                });

            migrationBuilder.CreateTable(
                name: "NoGoZones",
                schema: "dbo",
                columns: table => new
                {
                    NoGoZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Geometry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoGoZones", x => x.NoGoZoneId);
                });

            migrationBuilder.CreateTable(
                name: "PanicEvents",
                schema: "dbo",
                columns: table => new
                {
                    PanicEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PanicEvents", x => x.PanicEventId);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                schema: "dbo",
                columns: table => new
                {
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.FacultyId);
                    table.ForeignKey(
                        name: "FK_Faculties_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "dbo",
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PickupZones",
                schema: "dbo",
                columns: table => new
                {
                    PickupZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupZoneCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupZones", x => x.PickupZoneId);
                    table.ForeignKey(
                        name: "FK_PickupZones_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "dbo",
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PanicChatMessages",
                schema: "dbo",
                columns: table => new
                {
                    PanicChatMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PanicEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PanicEventId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PanicChatMessages", x => x.PanicChatMessageId);
                    table.ForeignKey(
                        name: "FK_PanicChatMessages_PanicEvents_PanicEventId",
                        column: x => x.PanicEventId,
                        principalSchema: "dbo",
                        principalTable: "PanicEvents",
                        principalColumn: "PanicEventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PanicChatMessages_PanicEvents_PanicEventId1",
                        column: x => x.PanicEventId1,
                        principalSchema: "dbo",
                        principalTable: "PanicEvents",
                        principalColumn: "PanicEventId");
                });

            migrationBuilder.CreateTable(
                name: "BoardingLogs",
                schema: "dbo",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RawUid = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServerTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Allowed = table.Column<bool>(type: "bit", nullable: false),
                    RouteMismatch = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOffline = table.Column<bool>(type: "bit", nullable: false),
                    IsTransferred = table.Column<bool>(type: "bit", nullable: false),
                    TransferReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BoardedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NFCCredentialCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardingLogs", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "BusRoutes",
                schema: "dbo",
                columns: table => new
                {
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusRoutes", x => x.RouteId);
                    table.ForeignKey(
                        name: "FK_BusRoutes_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "dbo",
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusRoutes_PickupZones_PickupZoneId",
                        column: x => x.PickupZoneId,
                        principalSchema: "dbo",
                        principalTable: "PickupZones",
                        principalColumn: "PickupZoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Residences",
                schema: "dbo",
                columns: table => new
                {
                    ResidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResidenceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResidenceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickupZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Residences", x => x.ResidenceId);
                    table.ForeignKey(
                        name: "FK_Residences_BusRoutes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "dbo",
                        principalTable: "BusRoutes",
                        principalColumn: "RouteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Residences_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "dbo",
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Residences_PickupZones_PickupZoneId",
                        column: x => x.PickupZoneId,
                        principalSchema: "dbo",
                        principalTable: "PickupZones",
                        principalColumn: "PickupZoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "dbo",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StudentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CellNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Students_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalSchema: "dbo",
                        principalTable: "Faculties",
                        principalColumn: "FacultyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "dbo",
                        principalTable: "Institutions",
                        principalColumn: "InstitutionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Residences_ResidenceId",
                        column: x => x.ResidenceId,
                        principalSchema: "dbo",
                        principalTable: "Residences",
                        principalColumn: "ResidenceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                schema: "dbo",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HardwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegisteredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TripDurationHours = table.Column<int>(type: "int", nullable: false),
                    LastConfigUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivationMode = table.Column<bool>(type: "bit", nullable: false),
                    PendingStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivationModeExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_Devices_Students_PendingStudentId",
                        column: x => x.PendingStudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "NFCCredentials",
                schema: "dbo",
                columns: table => new
                {
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CardSerial = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NFCCredentials", x => x.CredentialId);
                    table.ForeignKey(
                        name: "FK_NFCCredentials_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Placements",
                schema: "dbo",
                columns: table => new
                {
                    PlacementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PlacementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Placements", x => x.PlacementId);
                    table.ForeignKey(
                        name: "FK_Placements_BusRoutes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "dbo",
                        principalTable: "BusRoutes",
                        principalColumn: "RouteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Placements_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentAuths",
                schema: "dbo",
                columns: table => new
                {
                    AuthId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StudentId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAuths", x => x.AuthId);
                    table.ForeignKey(
                        name: "FK_StudentAuths_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAuths_Students_StudentId1",
                        column: x => x.StudentId1,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "StudentId");
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                schema: "dbo",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEmergencyEnded = table.Column<bool>(type: "bit", nullable: false),
                    EmergencyReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SwappedToTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SwappedFromTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduledEndTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.TripId);
                    table.ForeignKey(
                        name: "FK_Trips_BusRoutes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "dbo",
                        principalTable: "BusRoutes",
                        principalColumn: "RouteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Buses_BusId",
                        column: x => x.BusId,
                        principalSchema: "dbo",
                        principalTable: "Buses",
                        principalColumn: "BusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "dbo",
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Residences_ResidenceId",
                        column: x => x.ResidenceId,
                        principalSchema: "dbo",
                        principalTable: "Residences",
                        principalColumn: "ResidenceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trips_Trips_SwappedFromTripId",
                        column: x => x.SwappedFromTripId,
                        principalSchema: "dbo",
                        principalTable: "Trips",
                        principalColumn: "TripId");
                    table.ForeignKey(
                        name: "FK_Trips_Trips_SwappedToTripId",
                        column: x => x.SwappedToTripId,
                        principalSchema: "dbo",
                        principalTable: "Trips",
                        principalColumn: "TripId");
                });

            migrationBuilder.CreateTable(
                name: "TripTransfers",
                schema: "dbo",
                columns: table => new
                {
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DashboardUserUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripTransfers", x => x.TransferId);
                    table.ForeignKey(
                        name: "FK_TripTransfers_Buses_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "dbo",
                        principalTable: "Buses",
                        principalColumn: "BusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripTransfers_Buses_VehicleId1",
                        column: x => x.VehicleId1,
                        principalSchema: "dbo",
                        principalTable: "Buses",
                        principalColumn: "BusId");
                    table.ForeignKey(
                        name: "FK_TripTransfers_DashboardUsers_DashboardUserUserId",
                        column: x => x.DashboardUserUserId,
                        principalSchema: "dbo",
                        principalTable: "DashboardUsers",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_TripTransfers_DashboardUsers_TransferredBy",
                        column: x => x.TransferredBy,
                        principalSchema: "dbo",
                        principalTable: "DashboardUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TripTransfers_Trips_NewTripId",
                        column: x => x.NewTripId,
                        principalSchema: "dbo",
                        principalTable: "Trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripTransfers_Trips_OriginalTripId",
                        column: x => x.OriginalTripId,
                        principalSchema: "dbo",
                        principalTable: "Trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripTrip",
                schema: "dbo",
                columns: table => new
                {
                    InverseSwappedFromTripTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InverseSwappedToTripTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripTrip", x => new { x.InverseSwappedFromTripTripId, x.InverseSwappedToTripTripId });
                    table.ForeignKey(
                        name: "FK_TripTrip_Trips_InverseSwappedFromTripTripId",
                        column: x => x.InverseSwappedFromTripTripId,
                        principalSchema: "dbo",
                        principalTable: "Trips",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripTrip_Trips_InverseSwappedToTripTripId",
                        column: x => x.InverseSwappedToTripTripId,
                        principalSchema: "dbo",
                        principalTable: "Trips",
                        principalColumn: "TripId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardingLogs_CredentialId",
                schema: "dbo",
                table: "BoardingLogs",
                column: "CredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardingLogs_NFCCredentialCredentialId",
                schema: "dbo",
                table: "BoardingLogs",
                column: "NFCCredentialCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardingLogs_TripId",
                schema: "dbo",
                table: "BoardingLogs",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_BusRoutes_InstitutionId",
                schema: "dbo",
                table: "BusRoutes",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BusRoutes_PickupZoneId",
                schema: "dbo",
                table: "BusRoutes",
                column: "PickupZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_BusRoutes_ResidenceId",
                schema: "dbo",
                table: "BusRoutes",
                column: "ResidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_BusRoutes_RouteCode",
                schema: "dbo",
                table: "BusRoutes",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardUsers_Username",
                schema: "dbo",
                table: "DashboardUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceIdentifier",
                schema: "dbo",
                table: "Devices",
                column: "DeviceIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_PendingStudentId",
                schema: "dbo",
                table: "Devices",
                column: "PendingStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_StudentId",
                schema: "dbo",
                table: "Devices",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_InstitutionId",
                schema: "dbo",
                table: "Faculties",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_InstitutionCode",
                schema: "dbo",
                table: "Institutions",
                column: "InstitutionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NFCCredentials_CredentialToken",
                schema: "dbo",
                table: "NFCCredentials",
                column: "CredentialToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NFCCredentials_CredentialToken_CredentialType_IsActive",
                schema: "dbo",
                table: "NFCCredentials",
                columns: new[] { "CredentialToken", "CredentialType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NFCCredentials_StudentId",
                schema: "dbo",
                table: "NFCCredentials",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PanicChatMessages_PanicEventId",
                schema: "dbo",
                table: "PanicChatMessages",
                column: "PanicEventId");

            migrationBuilder.CreateIndex(
                name: "IX_PanicChatMessages_PanicEventId1",
                schema: "dbo",
                table: "PanicChatMessages",
                column: "PanicEventId1");

            migrationBuilder.CreateIndex(
                name: "IX_PickupZones_InstitutionId",
                schema: "dbo",
                table: "PickupZones",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupZones_PickupZoneCode",
                schema: "dbo",
                table: "PickupZones",
                column: "PickupZoneCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Placements_RouteId",
                schema: "dbo",
                table: "Placements",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Placements_StudentId",
                schema: "dbo",
                table: "Placements",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Residences_InstitutionId",
                schema: "dbo",
                table: "Residences",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Residences_PickupZoneId",
                schema: "dbo",
                table: "Residences",
                column: "PickupZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Residences_ResidenceCode",
                schema: "dbo",
                table: "Residences",
                column: "ResidenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Residences_RouteId",
                schema: "dbo",
                table: "Residences",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAuths_StudentId",
                schema: "dbo",
                table: "StudentAuths",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAuths_StudentId1",
                schema: "dbo",
                table: "StudentAuths",
                column: "StudentId1",
                unique: true,
                filter: "[StudentId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Students_FacultyId",
                schema: "dbo",
                table: "Students",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId",
                schema: "dbo",
                table: "Students",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ResidenceId",
                schema: "dbo",
                table: "Students",
                column: "ResidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                schema: "dbo",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_BusId",
                schema: "dbo",
                table: "Trips",
                column: "BusId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DeviceId",
                schema: "dbo",
                table: "Trips",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_ResidenceId",
                schema: "dbo",
                table: "Trips",
                column: "ResidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RouteId",
                schema: "dbo",
                table: "Trips",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_SwappedFromTripId",
                schema: "dbo",
                table: "Trips",
                column: "SwappedFromTripId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_SwappedToTripId",
                schema: "dbo",
                table: "Trips",
                column: "SwappedToTripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_DashboardUserUserId",
                schema: "dbo",
                table: "TripTransfers",
                column: "DashboardUserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_NewTripId",
                schema: "dbo",
                table: "TripTransfers",
                column: "NewTripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_OriginalTripId",
                schema: "dbo",
                table: "TripTransfers",
                column: "OriginalTripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_TransferredBy",
                schema: "dbo",
                table: "TripTransfers",
                column: "TransferredBy");

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_VehicleId",
                schema: "dbo",
                table: "TripTransfers",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_VehicleId1",
                schema: "dbo",
                table: "TripTransfers",
                column: "VehicleId1");

            migrationBuilder.CreateIndex(
                name: "IX_TripTrip_InverseSwappedToTripTripId",
                schema: "dbo",
                table: "TripTrip",
                column: "InverseSwappedToTripTripId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardingLogs_NFCCredentials_CredentialId",
                schema: "dbo",
                table: "BoardingLogs",
                column: "CredentialId",
                principalSchema: "dbo",
                principalTable: "NFCCredentials",
                principalColumn: "CredentialId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardingLogs_NFCCredentials_NFCCredentialCredentialId",
                schema: "dbo",
                table: "BoardingLogs",
                column: "NFCCredentialCredentialId",
                principalSchema: "dbo",
                principalTable: "NFCCredentials",
                principalColumn: "CredentialId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardingLogs_Trips_TripId",
                schema: "dbo",
                table: "BoardingLogs",
                column: "TripId",
                principalSchema: "dbo",
                principalTable: "Trips",
                principalColumn: "TripId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusRoutes_Residences_ResidenceId",
                schema: "dbo",
                table: "BusRoutes",
                column: "ResidenceId",
                principalSchema: "dbo",
                principalTable: "Residences",
                principalColumn: "ResidenceId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusRoutes_Institutions_InstitutionId",
                schema: "dbo",
                table: "BusRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupZones_Institutions_InstitutionId",
                schema: "dbo",
                table: "PickupZones");

            migrationBuilder.DropForeignKey(
                name: "FK_Residences_Institutions_InstitutionId",
                schema: "dbo",
                table: "Residences");

            migrationBuilder.DropForeignKey(
                name: "FK_BusRoutes_PickupZones_PickupZoneId",
                schema: "dbo",
                table: "BusRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_Residences_PickupZones_PickupZoneId",
                schema: "dbo",
                table: "Residences");

            migrationBuilder.DropForeignKey(
                name: "FK_BusRoutes_Residences_ResidenceId",
                schema: "dbo",
                table: "BusRoutes");

            migrationBuilder.DropTable(
                name: "BoardingLogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "NoGoZones",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PanicChatMessages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Placements",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "StudentAuths",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TripTransfers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TripTrip",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "NFCCredentials",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PanicEvents",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "DashboardUsers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Trips",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Buses",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Devices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Faculties",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Institutions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PickupZones",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Residences",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BusRoutes",
                schema: "dbo");
        }
    }
}
