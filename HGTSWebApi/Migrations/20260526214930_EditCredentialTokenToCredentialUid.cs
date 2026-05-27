using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HGTSWebApi.Migrations
{
    /// <inheritdoc />
    public partial class EditCredentialTokenToCredentialUid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CredentialToken",
                schema: "dbo",
                table: "NFCCredentials",
                newName: "CredentialUid");

            migrationBuilder.RenameIndex(
                name: "IX_NFCCredentials_CredentialToken_CredentialType_IsActive",
                schema: "dbo",
                table: "NFCCredentials",
                newName: "IX_NFCCredentials_CredentialUid_CredentialType_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_NFCCredentials_CredentialToken",
                schema: "dbo",
                table: "NFCCredentials",
                newName: "IX_NFCCredentials_CredentialUid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CredentialUid",
                schema: "dbo",
                table: "NFCCredentials",
                newName: "CredentialToken");

            migrationBuilder.RenameIndex(
                name: "IX_NFCCredentials_CredentialUid_CredentialType_IsActive",
                schema: "dbo",
                table: "NFCCredentials",
                newName: "IX_NFCCredentials_CredentialToken_CredentialType_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_NFCCredentials_CredentialUid",
                schema: "dbo",
                table: "NFCCredentials",
                newName: "IX_NFCCredentials_CredentialToken");
        }
    }
}
