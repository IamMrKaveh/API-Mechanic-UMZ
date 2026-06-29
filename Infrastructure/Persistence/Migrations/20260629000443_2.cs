using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrderStatuses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OrderStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_IsActive",
                table: "OrderStatuses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_IsDefault",
                table: "OrderStatuses",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_SortOrder",
                table: "OrderStatuses",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderStatuses_IsActive",
                table: "OrderStatuses");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatuses_IsDefault",
                table: "OrderStatuses");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatuses_SortOrder",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OrderStatuses");
        }
    }
}
