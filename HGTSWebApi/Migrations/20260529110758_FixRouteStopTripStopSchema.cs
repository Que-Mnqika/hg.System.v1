using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class FixRouteStopTripStopSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Move RouteStops from public to dbo schema
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS public.""RouteStops"" SET SCHEMA dbo;
            ");

            // Move TripStops from public to dbo schema
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS public.""TripStops"" SET SCHEMA dbo;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Move back to public schema if needed
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS dbo.""RouteStops"" SET SCHEMA public;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS dbo.""TripStops"" SET SCHEMA public;
            ");
        }
    }
}
