using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Spreadsheets;

public static class SheetEndpoints
{
    public static void MapSheetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spreadsheets/{spreadsheetId:guid}/sheets").RequireAuthorization();

        group.MapGet("", async (Guid spreadsheetId, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();

            var items = await db.Sheets.Where(s => s.SpreadsheetId == spreadsheetId)
                .OrderBy(s => s.Order)
                .Select(s => new SheetDto(s.Id, s.Name, s.Order))
                .ToListAsync();
            return Results.Ok(items);
        });

        group.MapPost("", async (Guid spreadsheetId, CreateSheetRequest request, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level == AccessLevel.View) return Results.NotFound();

            var nextOrder = await db.Sheets.Where(s => s.SpreadsheetId == spreadsheetId)
                .Select(s => (int?)s.Order).MaxAsync() ?? -1;

            var entity = new Sheet
            {
                Id = Guid.NewGuid(),
                SpreadsheetId = spreadsheetId,
                Name = request.Name,
                Order = nextOrder + 1
            };
            db.Sheets.Add(entity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/spreadsheets/{spreadsheetId}/sheets/{entity.Id}", new SheetDto(entity.Id, entity.Name, entity.Order));
        });

        // Sheet-scoped lookup: callers (e.g. the client) often know only a sheet id
        // (from the route) and need to resolve its owning spreadsheet id before they
        // can call spreadsheet-scoped endpoints such as sharing. This route is
        // intentionally top-level (not nested under /api/spreadsheets/{spreadsheetId})
        // since the spreadsheet id is exactly what's unknown to the caller.
        app.MapGet("/api/sheets/{id:guid}", async (Guid id, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var sheet = await db.Sheets.FirstOrDefaultAsync(s => s.Id == id);
            if (sheet is null) return Results.NotFound();

            var level = await access.GetAccessLevel(sheet.SpreadsheetId, userId);
            if (level is null) return Results.NotFound();

            return Results.Ok(new SheetSummaryDto(sheet.Id, sheet.SpreadsheetId, sheet.Name, sheet.Order));
        }).RequireAuthorization();
    }
}
