using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class _5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "TCartItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "TOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "TOrders",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentAuthority",
                table: "TOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentRefId",
                table: "TOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId_Color_Size",
                table: "TCartItems",
                columns: new[] { "CartId", "ProductId", "Color", "Size" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId_Color_Size",
                table: "TCartItems");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "PaymentAuthority",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "PaymentRefId",
                table: "TOrders");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "TCartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true);
        }
    }
}
