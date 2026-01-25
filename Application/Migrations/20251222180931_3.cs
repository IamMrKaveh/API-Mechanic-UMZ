using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Application.Migrations
{
    /// <inheritdoc />
    public partial class _3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippingMultiplier",
                table: "ProductVariants",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "ProductVariantShippingMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductVariantId = table.Column<int>(type: "integer", nullable: false),
                    ShippingMethodId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantShippingMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantShippingMethods_ProductVariants_ProductVarian~",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantShippingMethods_ShippingMethods_ShippingMetho~",
                        column: x => x.ShippingMethodId,
                        principalTable: "ShippingMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantShippingMethods_IsActive",
                table: "ProductVariantShippingMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantShippingMethods_ProductVariantId",
                table: "ProductVariantShippingMethods",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantShippingMethods_ProductVariantId_ShippingMeth~",
                table: "ProductVariantShippingMethods",
                columns: new[] { "ProductVariantId", "ShippingMethodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantShippingMethods_ShippingMethodId",
                table: "ProductVariantShippingMethods",
                column: "ShippingMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantShippingMethods");

            migrationBuilder.DropColumn(
                name: "ShippingMultiplier",
                table: "ProductVariants");
        }
    }
}
