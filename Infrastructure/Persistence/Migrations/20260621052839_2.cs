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
            migrationBuilder.DropForeignKey(
                name: "FK_WalletLedgerEntries_Users_UserId",
                table: "WalletLedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WalletLedgerEntries_Wallets_WalletId",
                table: "WalletLedgerEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletLedgerEntries_Users_UserId",
                table: "WalletLedgerEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WalletLedgerEntries_Wallets_WalletId",
                table: "WalletLedgerEntries",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletLedgerEntries_Users_UserId",
                table: "WalletLedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WalletLedgerEntries_Wallets_WalletId",
                table: "WalletLedgerEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletLedgerEntries_Users_UserId",
                table: "WalletLedgerEntries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WalletLedgerEntries_Wallets_WalletId",
                table: "WalletLedgerEntries",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
