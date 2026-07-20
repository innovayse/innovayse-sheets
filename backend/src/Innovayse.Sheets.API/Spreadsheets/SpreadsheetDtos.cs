namespace Innovayse.Sheets.API.Spreadsheets;

public record SpreadsheetDto(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string AccessLevel);
public record CreateSpreadsheetRequest(string Title);
public record RenameSpreadsheetRequest(string Title);
public record SheetDto(Guid Id, string Name, int Order);
public record CreateSheetRequest(string Name);
public record SheetSummaryDto(Guid Id, Guid SpreadsheetId, string Name, int Order);
