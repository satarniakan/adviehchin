using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adviehchin.App.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreCatalogSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExternalId",
                table: "StoreProducts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExternalId",
                table: "StoreCategories",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PriceTomans = table.Column<long>(type: "bigint", nullable: false),
                    DiscountPriceTomans = table.Column<long>(type: "bigint", nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreProductVariants_StoreProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "StoreProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_ExternalId",
                table: "StoreProducts",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCategories_ExternalId",
                table: "StoreCategories",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductVariants_ExternalId",
                table: "StoreProductVariants",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductVariants_IsDeleted",
                table: "StoreProductVariants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductVariants_ProductId_IsActive",
                table: "StoreProductVariants",
                columns: new[] { "ProductId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreProductVariants_ProductId_SortOrder",
                table: "StoreProductVariants",
                columns: new[] { "ProductId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_StoreProducts_ExternalId",
                table: "StoreProducts");

            migrationBuilder.DropIndex(
                name: "IX_StoreCategories_ExternalId",
                table: "StoreCategories");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "StoreCategories");
        }
    }
}
