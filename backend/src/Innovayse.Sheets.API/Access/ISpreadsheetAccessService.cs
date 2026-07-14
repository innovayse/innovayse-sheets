namespace Innovayse.Sheets.API.Access;

public interface ISpreadsheetAccessService
{
    Task<AccessLevel?> GetAccessLevel(Guid spreadsheetId, Guid userId);
}
