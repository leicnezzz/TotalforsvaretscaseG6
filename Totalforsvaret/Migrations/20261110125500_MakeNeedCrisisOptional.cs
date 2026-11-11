using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Totalforsvaret.Migrations
{
    public partial class MakeNeedCrisisOptional : Migration
    {
        private const string ForeignKey = "FK_Behov_Kriser_HendelseId";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ChangeCrisisColumn(migrationBuilder, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ChangeCrisisColumn(migrationBuilder, nullable: false);
        }

        private static void ChangeCrisisColumn(MigrationBuilder migrationBuilder, bool nullable)
        {
            migrationBuilder.DropForeignKey(ForeignKey, "Behov");

            migrationBuilder.AlterColumn<int>(
                name: "HendelseId",
                table: "Behov",
                type: "int",
                nullable: nullable,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: !nullable);

            migrationBuilder.AddForeignKey(
                name: ForeignKey,
                table: "Behov",
                column: "HendelseId",
                principalTable: "Kriser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
