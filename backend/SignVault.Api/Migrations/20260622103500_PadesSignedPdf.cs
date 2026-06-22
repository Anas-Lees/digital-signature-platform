using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignVault.Api.Migrations
{
    /// <inheritdoc />
    public partial class PadesSignedPdf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignatureBase64",
                table: "Signatures");

            migrationBuilder.AddColumn<string>(
                name: "SignedStorageKey",
                table: "Documents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedStorageKey",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "SignatureBase64",
                table: "Signatures",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
