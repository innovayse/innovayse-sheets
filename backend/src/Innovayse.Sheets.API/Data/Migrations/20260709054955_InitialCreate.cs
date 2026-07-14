using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innovayse.Sheets.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Col = table.Column<int>(type: "integer", nullable: false),
                    RawValue = table.Column<string>(type: "text", nullable: false),
                    FormatJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cells", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpreadsheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Spreadsheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spreadsheets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cells_SheetId_Row_Col",
                table: "Cells",
                columns: new[] { "SheetId", "Row", "Col" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sheets_SpreadsheetId",
                table: "Sheets",
                column: "SpreadsheetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cells");

            migrationBuilder.DropTable(
                name: "Sheets");

            migrationBuilder.DropTable(
                name: "Spreadsheets");
        }
    }
}
