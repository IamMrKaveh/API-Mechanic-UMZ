using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Required by every gin_trgm_ops index below.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Trigram indexes for unique columns (Email, Slug): EF cannot express a
            // second index on the same property as non-unique GIN, so these are raw.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Users_Email_trgm\" ON \"Users\" USING gin (\"Email\" gin_trgm_ops);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Products_Slug_trgm\" ON \"Products\" USING gin (\"Slug\" gin_trgm_ops);");

            migrationBuilder.DropTable(
                name: "RateLimitEntries");

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
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_processed_at",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                table: "Brands");

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

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequests_Status_CreatedAt",
                table: "WalletWithdrawalRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransfers_FromUserId_CompletedAt",
                table: "WalletTransfers",
                columns: new[] { "FromUserId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletTopUps_Status_CreatedAt",
                table: "WalletTopUps",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletLedgerEntries_UserId_ReferenceId",
                table: "WalletLedgerEntries",
                columns: new[] { "UserId", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsRevoked_ExpiresAt",
                table: "UserSessions",
                columns: new[] { "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_CreatedAt",
                table: "StockLedgerEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_EventType_CreatedAt",
                table: "StockLedgerEntries",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerEntries_ReferenceNumber",
                table: "StockLedgerEntries",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId_CreatedAt",
                table: "Products",
                columns: new[] { "BrandId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_CreatedAt",
                table: "Products",
                columns: new[] { "CategoryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_IsDeleted_CreatedAt",
                table: "Products",
                columns: new[] { "IsActive", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted_CreatedAt",
                table: "Products",
                columns: new[] { "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name_trgm",
                table: "Products",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_Comment_trgm",
                table: "ProductReviews",
                column: "Comment")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_Title_trgm",
                table: "ProductReviews",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Pending_ExpiresAt",
                table: "PaymentTransactions",
                column: "ExpiresAt",
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name_trgm",
                table: "Brands",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Details_trgm",
                table: "AuditLogs",
                column: "Details")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Unarchived",
                table: "AuditLogs",
                column: "IsArchived",
                filter: "\"IsArchived\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_Email_trgm\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Products_Slug_trgm\";");
            migrationBuilder.DropIndex(
                name: "IX_WalletWithdrawalRequests_Status_CreatedAt",
                table: "WalletWithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransfers_FromUserId_CompletedAt",
                table: "WalletTransfers");

            migrationBuilder.DropIndex(
                name: "IX_WalletTopUps_Status_CreatedAt",
                table: "WalletTopUps");

            migrationBuilder.DropIndex(
                name: "IX_WalletLedgerEntries_UserId_ReferenceId",
                table: "WalletLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_UserSessions_IsRevoked_ExpiresAt",
                table: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_CreatedAt",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_EventType_CreatedAt",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerEntries_ReferenceNumber",
                table: "StockLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_Products_BrandId_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_IsDeleted_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name_trgm",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_Comment_trgm",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_Title_trgm",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Pending_ExpiresAt",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name_trgm",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Details_trgm",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Unarchived",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AuditLogs",
                type: "timestamp(6) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "RateLimitEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    WindowKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitEntries", x => x.Id);
                });

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
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_processed_at",
                table: "OutboxMessages",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_HashVersion",
                table: "AuditLogs",
                column: "HashVersion");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsArchived",
                table: "AuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitEntries_Key_WindowKey",
                table: "RateLimitEntries",
                columns: new[] { "Key", "WindowKey" },
                unique: true);
        }
    }
}
