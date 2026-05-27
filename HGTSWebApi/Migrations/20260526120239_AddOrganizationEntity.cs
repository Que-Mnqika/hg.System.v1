using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "dbo",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "dbo",
                table: "Devices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "dbo",
                table: "DashboardUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.OrganizationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_OrganizationId",
                schema: "dbo",
                table: "Institutions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OrganizationId",
                schema: "dbo",
                table: "Devices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardUsers_OrganizationId",
                schema: "dbo",
                table: "DashboardUsers",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardUsers_Organization_OrganizationId",
                schema: "dbo",
                table: "DashboardUsers",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Organization_OrganizationId",
                schema: "dbo",
                table: "Devices",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_Organization_OrganizationId",
                schema: "dbo",
                table: "Institutions",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardUsers_Organization_OrganizationId",
                schema: "dbo",
                table: "DashboardUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Organization_OrganizationId",
                schema: "dbo",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_Organization_OrganizationId",
                schema: "dbo",
                table: "Institutions");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_OrganizationId",
                schema: "dbo",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Devices_OrganizationId",
                schema: "dbo",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_DashboardUsers_OrganizationId",
                schema: "dbo",
                table: "DashboardUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "dbo",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "dbo",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "dbo",
                table: "DashboardUsers");
        }
    }
}
