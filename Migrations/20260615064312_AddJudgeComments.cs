using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NamcyaTabulation.Migrations
{
    /// <inheritdoc />
    public partial class AddJudgeComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecimalPrecision",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LockScoring",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublishLeaderboard",
                table: "SystemSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "JudgeComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JudgeId = table.Column<int>(type: "int", nullable: false),
                    ContestantId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudgeComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JudgeComments_Contestants_ContestantId",
                        column: x => x.ContestantId,
                        principalTable: "Contestants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JudgeComments_Judges_JudgeId",
                        column: x => x.JudgeId,
                        principalTable: "Judges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeComments_ContestantId",
                table: "JudgeComments",
                column: "ContestantId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeComments_JudgeId",
                table: "JudgeComments",
                column: "JudgeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JudgeComments");

            migrationBuilder.DropColumn(
                name: "DecimalPrecision",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "LockScoring",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PublishLeaderboard",
                table: "SystemSettings");
        }
    }
}
