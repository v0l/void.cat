using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidCat.Migrations
{
    /// <inheritdoc />
    public partial class Nip96 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaDimensions",
                table: "Files",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalDigest",
                table: "Files",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_Digest",
                table: "Files",
                column: "Digest");

            migrationBuilder.CreateIndex(
                name: "IX_Files_OriginalDigest",
                table: "Files",
                column: "OriginalDigest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_Digest",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_OriginalDigest",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "MediaDimensions",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OriginalDigest",
                table: "Files");
        }
    }
}
