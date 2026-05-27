using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddActualStartTimeToTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartTime",
                schema: "dbo",
                table: "Trips",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualStartTime",
                schema: "dbo",
                table: "Trips");
        }
    }
}
