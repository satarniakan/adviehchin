using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adviehchin.App.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "StoreOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantTitle",
                table: "StoreOrderItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreOrderItems_VariantId",
                table: "StoreOrderItems",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreOrderItems_StoreProductVariants_VariantId",
                table: "StoreOrderItems",
                column: "VariantId",
                principalTable: "StoreProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreOrderItems_StoreProductVariants_VariantId",
                table: "StoreOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_StoreOrderItems_VariantId",
                table: "StoreOrderItems");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "StoreOrderItems");

            migrationBuilder.DropColumn(
                name: "VariantTitle",
                table: "StoreOrderItems");
        }
    }
}
