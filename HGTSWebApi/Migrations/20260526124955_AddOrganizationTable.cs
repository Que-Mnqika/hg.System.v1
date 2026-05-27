using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organization",
                table: "Organization");

            migrationBuilder.RenameTable(
                name: "Organization",
                newName: "Organizations");

            migrationBuilder.AlterColumn<string>(
                name: "Timezone",
                table: "Organizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Organizations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Code",
                table: "Organizations",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DashboardUsers_Organizations_OrganizationId",
                schema: "dbo",
                table: "DashboardUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Organizations_OrganizationId",
                schema: "dbo",
                table: "Devices",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_Organizations_OrganizationId",
                schema: "dbo",
                table: "Institutions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DashboardUsers_Organizations_OrganizationId",
                schema: "dbo",
                table: "DashboardUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Organizations_OrganizationId",
                schema: "dbo",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_Organizations_OrganizationId",
                schema: "dbo",
                table: "Institutions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Code",
                table: "Organizations");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "Organization");

            migrationBuilder.AlterColumn<string>(
                name: "Timezone",
                table: "Organization",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Organization",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organization",
                table: "Organization",
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
    }
}
