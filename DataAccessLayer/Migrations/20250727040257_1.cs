using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchasePrice = table.Column<int>(type: "int", nullable: true),
                    SellingPrice = table.Column<int>(type: "int", nullable: true),
                    Count = table.Column<int>(type: "int", nullable: true),
                    ProductTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TProducts_TProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "TProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<int>(type: "int", nullable: true),
                    TotalProfit = table.Column<int>(type: "int", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TOrders_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TOrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserOrderId = table.Column<int>(type: "int", nullable: true),
                    PurchasePrice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SellingPrice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Profit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TOrderDetails_TOrders_UserOrderId",
                        column: x => x.UserOrderId,
                        principalTable: "TOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TOrderDetailsTProducts",
                columns: table => new
                {
                    OrderDetailsId = table.Column<int>(type: "int", nullable: false),
                    ProductsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrderDetailsTProducts", x => new { x.OrderDetailsId, x.ProductsId });
                    table.ForeignKey(
                        name: "FK_TOrderDetailsTProducts_TOrderDetails_OrderDetailsId",
                        column: x => x.OrderDetailsId,
                        principalTable: "TOrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TOrderDetailsTProducts_TProducts_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "TProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TOrderDetails_UserOrderId",
                table: "TOrderDetails",
                column: "UserOrderId",
                unique: true,
                filter: "[UserOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TOrderDetailsTProducts_ProductsId",
                table: "TOrderDetailsTProducts",
                column: "ProductsId");

            migrationBuilder.CreateIndex(
                name: "IX_TOrders_UserId",
                table: "TOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TProducts_ProductTypeId",
                table: "TProducts",
                column: "ProductTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TOrderDetailsTProducts");

            migrationBuilder.DropTable(
                name: "TOrderDetails");

            migrationBuilder.DropTable(
                name: "TProducts");

            migrationBuilder.DropTable(
                name: "TOrders");

            migrationBuilder.DropTable(
                name: "TProductTypes");

            migrationBuilder.DropTable(
                name: "TUsers");
        }
    }
}
