using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adviehchin.App.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemPackaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Packaging",
                table: "StoreOrderItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Packaging",
                table: "StoreOrderItems");
        }
    }
}
