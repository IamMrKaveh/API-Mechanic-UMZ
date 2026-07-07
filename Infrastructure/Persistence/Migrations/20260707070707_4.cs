using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalletTopUps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Gateway = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GatewayAuthority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GatewayRefId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTopUps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_CreatedAt",
                table: "WalletTopUps",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_GatewayAuthority",
                table: "WalletTopUps",
                column: "GatewayAuthority",
                unique: true,
                filter: "\"GatewayAuthority\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_Status",
                table: "WalletTopUps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_UserId",
                table: "WalletTopUps",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTopUps");
        }
    }
}
