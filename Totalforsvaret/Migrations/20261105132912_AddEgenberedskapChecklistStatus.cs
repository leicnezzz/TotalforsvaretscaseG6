using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Totalforsvaret.Migrations
{
    /// <inheritdoc />
    public partial class AddEgenberedskapChecklistStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EgenberedskapChecklistStatuses",
                columns: table => new
                {
                    BrukerId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsComplete = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgenberedskapChecklistStatuses", x => new { x.BrukerId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_EgenberedskapChecklistStatuses_AspNetUsers_BrukerId",
                        column: x => x.BrukerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EgenberedskapChecklistStatuses");
        }
    }
}
