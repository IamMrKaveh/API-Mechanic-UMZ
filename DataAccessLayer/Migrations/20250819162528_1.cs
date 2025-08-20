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
                name: "TOrderStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrderStatus", x => x.Id);
                });

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
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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
                name: "TCarts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TCarts_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<int>(type: "int", nullable: false),
                    TotalProfit = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderStatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TOrders_TOrderStatus_OrderStatusId",
                        column: x => x.OrderStatusId,
                        principalTable: "TOrderStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TOrders_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TCartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TCartItems_TCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "TCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TCartItems_TProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TProducts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    PurchasePrice = table.Column<int>(type: "int", nullable: false),
                    SellingPrice = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Profit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TOrderItems_TOrders_UserOrderId",
                        column: x => x.UserOrderId,
                        principalTable: "TOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TOrderItems_TProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TProducts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_CartId",
                table: "TCartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_ProductId",
                table: "TCartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TCarts_UserId",
                table: "TCarts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TOrderItems_ProductId",
                table: "TOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TOrderItems_UserOrderId",
                table: "TOrderItems",
                column: "UserOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TOrders_OrderStatusId",
                table: "TOrders",
                column: "OrderStatusId");

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
                name: "TCartItems");

            migrationBuilder.DropTable(
                name: "TOrderItems");

            migrationBuilder.DropTable(
                name: "TCarts");

            migrationBuilder.DropTable(
                name: "TOrders");

            migrationBuilder.DropTable(
                name: "TProducts");

            migrationBuilder.DropTable(
                name: "TOrderStatus");

            migrationBuilder.DropTable(
                name: "TUsers");

            migrationBuilder.DropTable(
                name: "TProductTypes");
        }
    }
}
