using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Totalforsvaret.Migrations
{
    /// <inheritdoc />
    public partial class PublicProfileAndAccountDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kriser_AspNetUsers_OffentligAktorId",
                table: "Kriser");

            migrationBuilder.AlterColumn<int>(
                name: "OffentligAktørId",
                table: "Behov",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Kriser_AspNetUsers_OffentligAktorId",
                table: "Kriser",
                column: "OffentligAktorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kriser_AspNetUsers_OffentligAktorId",
                table: "Kriser");

            migrationBuilder.AlterColumn<int>(
                name: "OffentligAktørId",
                table: "Behov",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kriser_AspNetUsers_OffentligAktorId",
                table: "Kriser",
                column: "OffentligAktorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
