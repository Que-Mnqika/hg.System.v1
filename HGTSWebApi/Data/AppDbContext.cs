using Microsoft.EntityFrameworkCore;
using HGTSWebApi.Models;

namespace HGTSWebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Core Tables
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Residence> Residences { get; set; }
        public DbSet<VehicleRoute> VehicleRoutes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<NFCCredential> NFCCredentials { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<BoardingLog> BoardingLogs { get; set; }
        public DbSet<Placement> Placements { get; set; }
        public DbSet<Organization> Organizations { get; set; }

        // New Tables
        public DbSet<PickupZone> PickupZones { get; set; }
        public DbSet<NoGoZone> NoGoZones { get; set; }
        public DbSet<PanicEvent> PanicEvents { get; set; }
        public DbSet<PanicChatMessage> PanicChatMessages { get; set; }
        public DbSet<TripTransfer> TripTransfers { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<TripStop> TripStops { get; set; }

        // NEW: Manifest and Availability Tables
        public DbSet<VehicleAvailability> VehicleAvailabilities { get; set; }
        public DbSet<TripManifest> TripManifests { get; set; }
        public DbSet<TripManifestCredential> TripManifestCredentials { get; set; }
        public DbSet<TripManifestPassenger> TripManifestPassengers { get; set; }

        // Auth Tables
        public DbSet<DashboardUser> DashboardUsers { get; set; }
        public DbSet<StudentAuth> StudentAuths { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ ORGANIZATION ============
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.OrganizationId);
                entity.ToTable("Organizations", "dbo");
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Timezone).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
            });

            // ============ INSTITUTION ============
            modelBuilder.Entity<Institution>(entity =>
            {
                entity.HasKey(e => e.InstitutionId);
                entity.ToTable("Institutions", "dbo");
                entity.HasIndex(e => e.InstitutionCode).IsUnique();
                entity.Property(e => e.InstitutionName).HasMaxLength(100);
                entity.Property(e => e.InstitutionCode).HasMaxLength(10);
                entity.Property(e => e.CampusName).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(20);

                // Add relationship with Organization
                entity.HasOne(d => d.Organization)
                    .WithMany(p => p.Institutions)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ FACULTY ============
            modelBuilder.Entity<Faculty>(entity =>
            {
                entity.HasKey(e => e.FacultyId);
                entity.ToTable("Faculties", "dbo");
                entity.Property(e => e.FacultyName).HasMaxLength(100);

                entity.HasOne(d => d.Institution)
                    .WithMany(p => p.Faculties)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ PICKUP ZONE ============
            modelBuilder.Entity<PickupZone>(entity =>
            {
                entity.HasKey(e => e.PickupZoneId);
                entity.ToTable("PickupZones", "dbo");
                entity.HasIndex(e => e.PickupZoneCode).IsUnique();
                entity.Property(e => e.PickupZoneCode).HasMaxLength(10);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);

                entity.HasOne(d => d.Institution)
                    .WithMany(p => p.PickupZones)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ RESIDENCE ============
            modelBuilder.Entity<Residence>(entity =>
            {
                entity.HasKey(e => e.ResidenceId);
                entity.ToTable("Residences", "dbo");
                entity.HasIndex(e => e.ResidenceCode).IsUnique();
                entity.Property(e => e.ResidenceCode).HasMaxLength(20);
                entity.Property(e => e.ResidenceName).HasMaxLength(100);
                entity.Property(e => e.Timezone).HasMaxLength(50);

                entity.HasOne(d => d.Institution)
                    .WithMany(p => p.Residences)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.PickupZone)
                    .WithMany(p => p.Residences)
                    .HasForeignKey(d => d.PickupZoneId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Route)
                    .WithMany(p => p.Residences)
                    .HasForeignKey(d => d.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ VEHICLE ROUTE ============
            modelBuilder.Entity<VehicleRoute>(entity =>
            {
                entity.HasKey(e => e.RouteId);
                entity.ToTable("BusRoutes", "dbo");
                entity.HasIndex(e => e.RouteCode).IsUnique();
                entity.Property(e => e.RouteCode).HasMaxLength(20);
                entity.Property(e => e.RouteName).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);

                entity.HasOne(d => d.Institution)
                    .WithMany(p => p.Routes)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.PickupZone)
                    .WithMany(p => p.Routes)
                    .HasForeignKey(d => d.PickupZoneId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Residence)
                    .WithMany()
                    .HasForeignKey(d => d.ResidenceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.Residences)
                    .WithOne(r => r.Route)
                    .HasForeignKey(r => r.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ VEHICLE ============
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.VehicleId);
                entity.ToTable("Buses", "dbo");
                entity.Property(e => e.VehicleId).HasColumnName("BusId");
                entity.Property(e => e.RegistrationNumber).HasColumnName("BusLabel").HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Capacity).HasDefaultValue(50);
            });

            // ============ STUDENT ============
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId);
                entity.ToTable("Students", "dbo");
                entity.HasIndex(e => e.StudentNumber).IsUnique();
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.StudentNumber).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.CellNumber).HasMaxLength(20);
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasOne(d => d.Institution)
                    .WithMany(p => p.Students)
                    .HasForeignKey(d => d.InstitutionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Residence)
                    .WithMany(p => p.Students)
                    .HasForeignKey(d => d.ResidenceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Faculty)
                    .WithMany(p => p.Students)
                    .HasForeignKey(d => d.FacultyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ NFC CREDENTIAL ============
            modelBuilder.Entity<NFCCredential>(entity =>
            {
                entity.HasKey(e => e.CredentialId);
                entity.ToTable("NFCCredentials", "dbo");
                entity.HasIndex(e => e.CredentialUid).IsUnique();
                entity.HasIndex(e => new { e.CredentialUid, e.CredentialType, e.IsActive });
                entity.Property(e => e.CredentialUid).HasMaxLength(255);
                entity.Property(e => e.CredentialType).HasMaxLength(20); // Increased to 20

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Credentials)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ DEVICE ============
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.DeviceId);
                entity.ToTable("Devices", "dbo");
                entity.HasIndex(e => e.DeviceIdentifier).IsUnique();
                entity.Property(e => e.DeviceIdentifier).HasMaxLength(100);
                entity.Property(e => e.DeviceName).HasMaxLength(100);
                entity.Property(e => e.FirmwareVersion).HasMaxLength(50);
                entity.Property(e => e.HardwareVersion).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Location).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);

                entity.HasOne(d => d.PendingStudent)
                    .WithMany()
                    .HasForeignKey(d => d.PendingStudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ TRIP ============
            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(e => e.TripId);
                entity.ToTable("Trips", "dbo");
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.EmergencyReason).HasMaxLength(200);
                entity.Property(e => e.VehicleId).HasColumnName("BusId");

                entity.Property(e => e.ScheduledStartTime).IsRequired(false);
                entity.Property(e => e.ScheduledEndTime).IsRequired(false);

                entity.HasOne(d => d.BusRoute)
                    .WithMany(p => p.Trips)
                    .HasForeignKey(d => d.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.Trips)
                    .HasForeignKey(d => d.DeviceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Vehicle)
                    .WithMany(p => p.Trips)
                    .HasForeignKey(d => d.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Residence)
                    .WithMany(p => p.Trips)
                    .HasForeignKey(d => d.ResidenceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.SwappedFromTrip)
                    .WithMany()
                    .HasForeignKey(d => d.SwappedFromTripId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.SwappedToTrip)
                    .WithMany()
                    .HasForeignKey(d => d.SwappedToTripId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ============ BOARDING LOG ============
            modelBuilder.Entity<BoardingLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.ToTable("BoardingLogs", "dbo");
                entity.Property(e => e.DeviceId).HasMaxLength(100);
                entity.Property(e => e.CredentialUid).HasMaxLength(50);
                entity.Property(e => e.Result).HasMaxLength(50);
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.TransferReason).HasMaxLength(50);

                entity.HasOne(d => d.Credential)
                    .WithMany()
                    .HasForeignKey(d => d.CredentialId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Trip)
                    .WithMany(p => p.BoardingLogs)
                    .HasForeignKey(d => d.TripId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ PLACEMENT ============
            modelBuilder.Entity<Placement>(entity =>
            {
                entity.HasKey(e => e.PlacementId);
                entity.ToTable("Placements", "dbo");
                entity.Property(e => e.LocationName).HasMaxLength(100);
                entity.Property(e => e.LocationAddress).HasMaxLength(200);
                entity.Property(e => e.PlacementType).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Placements)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Route)
                    .WithMany(p => p.Placements)
                    .HasForeignKey(d => d.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ DASHBOARD USER ============
            modelBuilder.Entity<DashboardUser>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.ToTable("DashboardUsers", "dbo");
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Role).HasMaxLength(50);
                entity.Property(e => e.UserCategory).HasMaxLength(50);
                entity.Property(e => e.Department).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            });

            // ============ STUDENT AUTH ============
            modelBuilder.Entity<StudentAuth>(entity =>
            {
                entity.HasKey(e => e.AuthId);
                entity.ToTable("StudentAuths", "dbo");
                entity.HasIndex(e => e.StudentId).IsUnique();

                entity.HasOne(d => d.Student)
                    .WithOne(s => s.StudentAuth)
                    .HasForeignKey<StudentAuth>(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ NO GO ZONE ============
            modelBuilder.Entity<NoGoZone>(entity =>
            {
                entity.HasKey(e => e.NoGoZoneId);
                entity.ToTable("NoGoZones", "dbo");
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Reason).HasMaxLength(255);
            });

            // ============ PANIC EVENT ============
            modelBuilder.Entity<PanicEvent>(entity =>
            {
                entity.HasKey(e => e.PanicEventId);
                entity.ToTable("PanicEvents", "dbo");
            });

            // ============ PANIC CHAT MESSAGE ============
            modelBuilder.Entity<PanicChatMessage>(entity =>
            {
                entity.HasKey(e => e.PanicChatMessageId);
                entity.ToTable("PanicChatMessages", "dbo");

                entity.HasOne(d => d.PanicEvent)
                    .WithMany()
                    .HasForeignKey(d => d.PanicEventId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ TRIP TRANSFER ============
            modelBuilder.Entity<TripTransfer>(entity =>
            {
                entity.HasKey(e => e.TransferId);
                entity.ToTable("TripTransfers", "dbo");
                entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.OriginalTrip)
                    .WithMany(t => t.TripTransferOriginalTrips)
                    .HasForeignKey(e => e.OriginalTripId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.NewTrip)
                    .WithMany(t => t.TripTransferNewTrips)
                    .HasForeignKey(e => e.NewTripId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TransferredByNavigation)
                    .WithMany(u => u.TripTransferTransferredByNavigations)
                    .HasForeignKey(e => e.TransferredBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ============ ROUTE STOP ============
            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.HasKey(rs => rs.RouteStopId);
                entity.ToTable("RouteStops", "dbo");
                entity.Property(rs => rs.StopOrder).IsRequired();

                entity.HasOne(rs => rs.Route)
                      .WithMany(r => r.RouteStops)
                      .HasForeignKey(rs => rs.RouteId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Residence)
                      .WithMany()
                      .HasForeignKey(rs => rs.ResidenceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ TRIP STOP ============
            modelBuilder.Entity<TripStop>(entity =>
            {
                entity.HasKey(ts => ts.TripStopId);
                entity.ToTable("TripStops", "dbo");

                entity.HasOne(ts => ts.Trip)
                      .WithMany(t => t.TripStops)
                      .HasForeignKey(ts => ts.TripId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ts => ts.RouteStop)
                      .WithMany(rs => rs.TripStops)
                      .HasForeignKey(ts => ts.RouteStopId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ VEHICLE AVAILABILITY (NEW) ============
            modelBuilder.Entity<VehicleAvailability>(entity =>
            {
                entity.HasKey(e => e.VehicleAvailabilityId);
                entity.ToTable("VehicleAvailabilities", "dbo");
                entity.Property(e => e.Reason).HasMaxLength(255);
                entity.Property(e => e.Date).HasColumnType("date");

                entity.HasIndex(e => new { e.VehicleId, e.Date }).IsUnique();

                entity.HasOne(e => e.Vehicle)
                    .WithMany()
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ TRIP MANIFEST (NEW) ============
            modelBuilder.Entity<TripManifest>(entity =>
            {
                entity.HasKey(e => e.TripManifestId);
                entity.ToTable("TripManifests", "dbo");
                entity.Property(e => e.Status).HasMaxLength(20);

                entity.HasIndex(e => e.TripId).IsUnique();

                entity.HasOne(e => e.Trip)
                    .WithOne()
                    .HasForeignKey<TripManifest>(e => e.TripId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ TRIP MANIFEST CREDENTIAL (NEW) ============
            modelBuilder.Entity<TripManifestCredential>(entity =>
            {
                entity.HasKey(e => e.TripManifestCredentialId);
                entity.ToTable("TripManifestCredentials", "dbo");
                entity.Property(e => e.CredentialUid).HasMaxLength(255);
                entity.Property(e => e.CredentialType).HasMaxLength(20);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.StudentNumber).HasMaxLength(50);

                entity.HasIndex(e => e.CredentialUid);
                entity.HasIndex(e => e.StudentId);

                entity.HasOne(e => e.TripManifest)
                    .WithMany(m => m.Credentials)
                    .HasForeignKey(e => e.TripManifestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Residence)
                    .WithMany()
                    .HasForeignKey(e => e.ResidenceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ TRIP MANIFEST PASSENGER (NEW) ============
            modelBuilder.Entity<TripManifestPassenger>(entity =>
            {
                entity.HasKey(e => e.TripManifestPassengerId);
                entity.ToTable("TripManifestPassengers", "dbo");
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.StudentNumber).HasMaxLength(50);

                entity.HasIndex(e => e.StudentId);
                entity.HasIndex(e => e.HasBoarded);

                entity.HasOne(e => e.TripManifest)
                    .WithMany(m => m.Passengers)
                    .HasForeignKey(e => e.TripManifestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Residence)
                    .WithMany()
                    .HasForeignKey(e => e.ResidenceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}