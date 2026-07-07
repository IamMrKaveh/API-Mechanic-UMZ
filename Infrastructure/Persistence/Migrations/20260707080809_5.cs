using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantShippings_ProductVariants_VariantId",
                table: "ProductVariantShippings");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariantShippings_Shippings_ShippingId",
                table: "ProductVariantShippings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductVariantShippings",
                table: "ProductVariantShippings");

            migrationBuilder.RenameTable(
                name: "ProductVariantShippings",
                newName: "VariantShippings");

            migrationBuilder.RenameIndex(
                name: "IX_ProductVariantShippings_VariantId",
                table: "VariantShippings",
                newName: "IX_VariantShippings_VariantId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductVariantShippings_ShippingId",
                table: "VariantShippings",
                newName: "IX_VariantShippings_ShippingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VariantShippings",
                table: "VariantShippings",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "WalletWithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Iban = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccountHolder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BankReferenceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletWithdrawalRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_CreatedAt",
                table: "WalletWithdrawalRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_ReservationId",
                table: "WalletWithdrawalRequests",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_Status",
                table: "WalletWithdrawalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_UserId",
                table: "WalletWithdrawalRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantShippings_ProductVariants_VariantId",
                table: "VariantShippings",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantShippings_Shippings_ShippingId",
                table: "VariantShippings",
                column: "ShippingId",
                principalTable: "Shippings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantShippings_ProductVariants_VariantId",
                table: "VariantShippings");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantShippings_Shippings_ShippingId",
                table: "VariantShippings");

            migrationBuilder.DropTable(
                name: "WalletWithdrawalRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VariantShippings",
                table: "VariantShippings");

            migrationBuilder.RenameTable(
                name: "VariantShippings",
                newName: "ProductVariantShippings");

            migrationBuilder.RenameIndex(
                name: "IX_VariantShippings_VariantId",
                table: "ProductVariantShippings",
                newName: "IX_ProductVariantShippings_VariantId");

            migrationBuilder.RenameIndex(
                name: "IX_VariantShippings_ShippingId",
                table: "ProductVariantShippings",
                newName: "IX_ProductVariantShippings_ShippingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductVariantShippings",
                table: "ProductVariantShippings",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantShippings_ProductVariants_VariantId",
                table: "ProductVariantShippings",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariantShippings_Shippings_ShippingId",
                table: "ProductVariantShippings",
                column: "ShippingId",
                principalTable: "Shippings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
