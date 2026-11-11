using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adviehchin.App.Migrations
{
    /// <inheritdoc />
    public partial class AddManualProductDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyDeactivated",
                table: "StoreProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManuallyDeactivated",
                table: "StoreProducts");
        }
    }
}
