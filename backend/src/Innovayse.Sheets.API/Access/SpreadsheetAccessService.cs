using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Access;

public class SpreadsheetAccessService : ISpreadsheetAccessService
{
    private readonly SheetsDbContext _db;

    public SpreadsheetAccessService(SheetsDbContext db)
    {
        _db = db;
    }

    public async Task<AccessLevel?> GetAccessLevel(Guid spreadsheetId, Guid userId)
    {
        var spreadsheet = await _db.Spreadsheets.FirstOrDefaultAsync(s => s.Id == spreadsheetId);
        if (spreadsheet is null) return null;

        if (spreadsheet.OwnerId == userId) return AccessLevel.Owner;

        var share = await _db.Shares.FirstOrDefaultAsync(s => s.SpreadsheetId == spreadsheetId && s.UserId == userId);
        if (share is null) return null;

        return share.Role switch
        {
            ShareRole.Edit => AccessLevel.Edit,
            ShareRole.View => AccessLevel.View,
            _ => null
        };
    }
}
