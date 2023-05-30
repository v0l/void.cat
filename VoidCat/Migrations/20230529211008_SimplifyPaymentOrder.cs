using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoidCat.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPaymentOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentStrike");

            migrationBuilder.AddColumn<string>(
                name: "Upstream",
                table: "Payment",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Upstream",
                table: "Payment");

            migrationBuilder.CreateTable(
                name: "PaymentStrike",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Handle = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStrike", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentStrike_Payment_Id",
                        column: x => x.Id,
                        principalTable: "Payment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
