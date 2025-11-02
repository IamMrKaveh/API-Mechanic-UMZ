using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class _4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TOrderStatus",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TOrderStatus",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TOrderStatus",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TOrderStatus",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TOrderStatus",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TOrderStatus",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
