using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Totalforsvaret.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCrisisAndNeedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OffentligAktorId",
                table: "Kriser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                    name: "Type",
                    table: "Kriser",
                    type: "longtext",
                    nullable: false,
                    defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Kriser_OffentligAktorId",
                table: "Kriser",
                column: "OffentligAktorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kriser_AspNetUsers_OffentligAktorId",
                table: "Kriser",
                column: "OffentligAktorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kriser_AspNetUsers_OffentligAktorId",
                table: "Kriser");

            migrationBuilder.DropIndex(
                name: "IX_Kriser_OffentligAktorId",
                table: "Kriser");

            migrationBuilder.DropColumn(
                name: "OffentligAktorId",
                table: "Kriser");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Kriser");
        }
    }
}
