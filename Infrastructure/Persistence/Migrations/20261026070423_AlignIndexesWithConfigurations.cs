using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignIndexesWithConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Second index on IsArchived next to the filtered IX_AuditLogs_Unarchived
            // index (20261024020100): the filter stays, a plain index is added.
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsArchived",
                table: "AuditLogs",
                column: "IsArchived");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AuditLogs",
                type: "timestamp(6) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_CreatedAt",
                table: "WalletWithdrawalRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_Status",
                table: "WalletWithdrawalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransfers_CreatedAt",
                table: "WalletTransfers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransfers_Status",
                table: "WalletTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_CreatedAt",
                table: "WalletTopUps",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_Status",
                table: "WalletTopUps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status",
                table: "Tickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributes_VariantId",
                table: "ProductVariantAttributes",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_HashVersion",
                table: "AuditLogs",
                column: "HashVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletWithdrawalRequests_CreatedAt",
                table: "WalletWithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WalletWithdrawalRequests_Status",
                table: "WalletWithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransfers_CreatedAt",
                table: "WalletTransfers");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransfers_Status",
                table: "WalletTransfers");

            migrationBuilder.DropIndex(
                name: "IX_WalletTopUps_CreatedAt",
                table: "WalletTopUps");

            migrationBuilder.DropIndex(
                name: "IX_WalletTopUps_Status",
                table: "WalletTopUps");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariantAttributes_VariantId",
                table: "ProductVariantAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_HashVersion",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IsArchived",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6) with time zone");
        }
    }
}
