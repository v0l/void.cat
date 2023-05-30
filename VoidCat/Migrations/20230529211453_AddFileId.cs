using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidCat.Migrations
{
    /// <inheritdoc />
    public partial class AddFileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_Files_Id",
                table: "Payment");

            migrationBuilder.AddColumn<Guid>(
                name: "FileId",
                table: "Payment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payment_FileId",
                table: "Payment",
                column: "FileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_Files_FileId",
                table: "Payment",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_Files_FileId",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_FileId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Payment");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_Files_Id",
                table: "Payment",
                column: "Id",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
