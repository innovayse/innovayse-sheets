using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innovayse.Sheets.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Cells_Sheets_SheetId",
                table: "Cells",
                column: "SheetId",
                principalTable: "Sheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sheets_Spreadsheets_SpreadsheetId",
                table: "Sheets",
                column: "SpreadsheetId",
                principalTable: "Spreadsheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cells_Sheets_SheetId",
                table: "Cells");

            migrationBuilder.DropForeignKey(
                name: "FK_Sheets_Spreadsheets_SpreadsheetId",
                table: "Sheets");
        }
    }
}
