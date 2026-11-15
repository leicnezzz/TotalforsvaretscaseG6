using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Totalforsvaret.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceMessages_ResourceAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "ResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceMessages_AssignmentId_SentAtUtc",
                table: "ResourceMessages",
                columns: new[] { "AssignmentId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceMessages_SenderUserId",
                table: "ResourceMessages",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceMessages");
        }
    }
}
