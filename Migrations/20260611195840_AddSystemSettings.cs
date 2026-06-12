using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamcyaTabulation.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPublish",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DecimalScoring",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "OrgName",
                table: "SystemSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPublish",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "SystemSettings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "DecimalScoring",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrgName",
                table: "SystemSettings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
