using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Totalforsvaret.Migrations
{
    /// <inheritdoc />
    public partial class Mappe1Revisjon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Organisasjonnummer",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrertDato",
                table: "Resources",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.CreateIndex(
                name: "IX_Behov_OffentligAktørId",
                table: "Behov",
                column: "OffentligAktørId");

            migrationBuilder.AddForeignKey(
                name: "FK_Behov_AspNetUsers_OffentligAktørId",
                table: "Behov",
                column: "OffentligAktørId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Behov_AspNetUsers_OffentligAktørId",
                table: "Behov");

            migrationBuilder.DropIndex(
                name: "IX_Behov_OffentligAktørId",
                table: "Behov");

            migrationBuilder.DropColumn(
                name: "RegistrertDato",
                table: "Resources");

            migrationBuilder.AddColumn<string>(
                name: "Organisasjonnummer",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
