using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrderStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TRateLimit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    ResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRateLimit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TRateLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRateLimits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    PurchasePrice = table.Column<int>(type: "integer", nullable: true),
                    OriginalPrice = table.Column<int>(type: "integer", nullable: true),
                    SellingPrice = table.Column<int>(type: "integer", nullable: true),
                    Count = table.Column<int>(type: "integer", nullable: true),
                    ProductTypeId = table.Column<int>(type: "integer", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    TotalAmount = table.Column<int>(type: "integer", nullable: false),
                    TotalProfit = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderStatusId = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
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
                name: "TRefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "text", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TRefreshToken_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TUserOtps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OtpHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('UTC', NOW())"),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TUserOtps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TUserOtps_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TCartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<int>(type: "integer", nullable: false),
                    SellingPrice = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Profit = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_CartItems_CartId_ProductId",
                table: "TCartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_ProductId",
                table: "TCartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId_Unique",
                table: "TCarts",
                column: "UserId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_TRateLimit_Key",
                table: "TRateLimit",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TRefreshToken_UserId",
                table: "TRefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TUserOtps_UserId",
                table: "TUserOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TUsers_PhoneNumber",
                table: "TUsers",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TCartItems");

            migrationBuilder.DropTable(
                name: "TOrderItems");

            migrationBuilder.DropTable(
                name: "TRateLimit");

            migrationBuilder.DropTable(
                name: "TRateLimits");

            migrationBuilder.DropTable(
                name: "TRefreshToken");

            migrationBuilder.DropTable(
                name: "TUserOtps");

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
