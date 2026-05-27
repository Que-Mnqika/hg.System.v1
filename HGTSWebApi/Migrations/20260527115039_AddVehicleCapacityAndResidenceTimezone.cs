using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCapacityAndResidenceTimezone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TripTransfers_Buses_VehicleId1",
                schema: "dbo",
                table: "TripTransfers");

            migrationBuilder.DropIndex(
                name: "IX_TripTransfers_VehicleId1",
                schema: "dbo",
                table: "TripTransfers");

            migrationBuilder.DropColumn(
                name: "VehicleId1",
                schema: "dbo",
                table: "TripTransfers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "dbo",
                table: "Residences");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "dbo",
                table: "Residences");

            migrationBuilder.RenameColumn(
                name: "Address",
                schema: "dbo",
                table: "Residences",
                newName: "Timezone");

            migrationBuilder.RenameColumn(
                name: "RawUid",
                schema: "dbo",
                table: "BoardingLogs",
                newName: "CredentialUid");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleName",
                schema: "dbo",
                table: "Buses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                schema: "dbo",
                table: "Buses",
                type: "int",
                nullable: false,
                defaultValue: 50,
                oldClrType: typeof(int),
                oldType: "int");
            // 1. Add Capacity column to Buses table
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Buses",
                type: "int",
                nullable: false,
                defaultValue: 50);

            // 2. Add Timezone column to Residences table
            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "Residences",
                type: "nvarchar(50)",
                nullable: true,
                defaultValue: "UTC");

            // 3. Increase CredentialType column size (fix for truncation error)
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[NFCCredentials] 
                ALTER COLUMN [CredentialType] NVARCHAR(20) NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timezone",
                schema: "dbo",
                table: "Residences",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "CredentialUid",
                schema: "dbo",
                table: "BoardingLogs",
                newName: "RawUid");

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId1",
                schema: "dbo",
                table: "TripTransfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "dbo",
                table: "Residences",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "dbo",
                table: "Residences",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "VehicleName",
                schema: "dbo",
                table: "Buses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                schema: "dbo",
                table: "Buses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 50);

            migrationBuilder.CreateIndex(
                name: "IX_TripTransfers_VehicleId1",
                schema: "dbo",
                table: "TripTransfers",
                column: "VehicleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TripTransfers_Buses_VehicleId1",
                schema: "dbo",
                table: "TripTransfers",
                column: "VehicleId1",
                principalSchema: "dbo",
                principalTable: "Buses",
                principalColumn: "BusId");

            // 1. Remove Capacity column
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Buses");

            // 2. Remove Timezone column
            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "Residences");

            // 3. Revert CredentialType back to NVARCHAR(10)
            migrationBuilder.Sql(@"
                ALTER TABLE [dbo].[NFCCredentials] 
                ALTER COLUMN [CredentialType] NVARCHAR(10) NOT NULL
            ");
        }
    }
}
