using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignVault.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSignerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignerName",
                table: "Signatures",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignerName",
                table: "Signatures");
        }
    }
}
