using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adviehchin.App.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreAboutPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreAboutPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Lead = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    StoryTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StoryBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAboutPages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreAboutPages");
        }
    }
}
