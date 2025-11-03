using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductCount = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<long>(type: "bigint", nullable: false),
                    InStockProducts = table.Column<int>(type: "integer", nullable: false),
                    TotalSellingValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TOrderStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrderStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TRateLimit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    LastAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRateLimit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Colors = table.Column<string[]>(type: "text[]", nullable: false),
                    Sizes = table.Column<string[]>(type: "text[]", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsUnlimited = table.Column<bool>(type: "boolean", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TProducts_TCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TCarts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TCarts_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TotalAmount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalProfit = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderStatusId = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentAuthority = table.Column<string>(type: "text", nullable: true),
                    PaymentRefId = table.Column<long>(type: "bigint", nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TOrders_TOrderStatus_OrderStatusId",
                        column: x => x.OrderStatusId,
                        principalTable: "TOrderStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOrders_TUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TRefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
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
                name: "TUserOtp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OtpHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TUserOtp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TUserOtp_TUsers_UserId",
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
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    Profit = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TOrderStatus",
                columns: new[] { "Id", "Icon", "Name" },
                values: new object[,]
                {
                    { 1, "hourglass_empty", "در انتظار پرداخت" },
                    { 2, "sync", "در حال پردازش" },
                    { 3, "local_shipping", "ارسال شده" },
                    { 4, "done_all", "تحویل داده شده" },
                    { 5, "cancel", "لغو شده" },
                    { 6, "assignment_return", "مرجوعی" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TAuditLogs_Timestamp",
                table: "TAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TAuditLogs_UserId_EventType_Timestamp",
                table: "TAuditLogs",
                columns: new[] { "UserId", "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId_Color_Size",
                table: "TCartItems",
                columns: new[] { "CartId", "ProductId", "Color", "Size" },
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
                name: "IX_TOrders_UserId_IdempotencyKey",
                table: "TOrders",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TProducts_CategoryId",
                table: "TProducts",
                column: "CategoryId");

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
                name: "IX_TUserOtp_UserId",
                table: "TUserOtp",
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
                name: "TAuditLogs");

            migrationBuilder.DropTable(
                name: "TCartItems");

            migrationBuilder.DropTable(
                name: "TOrderItems");

            migrationBuilder.DropTable(
                name: "TRateLimit");

            migrationBuilder.DropTable(
                name: "TRefreshToken");

            migrationBuilder.DropTable(
                name: "TUserOtp");

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
                name: "TCategory");
        }
    }
}
