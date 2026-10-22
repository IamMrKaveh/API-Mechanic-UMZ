using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Dispatch",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<int>(
                name: "sequence",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Dispatch",
                table: "OutboxMessages",
                columns: new[] { "processed_at", "is_poisoned", "retry_count", "created_at", "sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Dispatch",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "sequence",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Dispatch",
                table: "OutboxMessages",
                columns: new[] { "processed_at", "is_poisoned", "retry_count", "created_at" });
        }
    }
}
