using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Spreadsheets;

public static class SpreadsheetEndpoints
{
    public static void MapSpreadsheetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spreadsheets").RequireAuthorization();

        group.MapGet("", async (SheetsDbContext db, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var owned = await db.Spreadsheets.Where(s => s.OwnerId == userId)
                .Select(s => new SpreadsheetDto(s.Id, s.Title, s.CreatedAt, s.UpdatedAt, "Owner"))
                .ToListAsync();

            var sharedIds = await db.Shares.Where(sh => sh.UserId == userId)
                .Select(sh => new { sh.SpreadsheetId, sh.Role })
                .ToListAsync();

            var shared = new List<SpreadsheetDto>();
            foreach (var s in sharedIds)
            {
                var spreadsheet = await db.Spreadsheets.FirstOrDefaultAsync(sp => sp.Id == s.SpreadsheetId);
                if (spreadsheet is not null)
                {
                    shared.Add(new SpreadsheetDto(spreadsheet.Id, spreadsheet.Title, spreadsheet.CreatedAt, spreadsheet.UpdatedAt, s.Role.ToString()));
                }
            }

            return Results.Ok(owned.Concat(shared));
        });

        group.MapPost("", async (CreateSpreadsheetRequest request, SheetsDbContext db, HttpContext ctx) =>
        {
            var ownerId = CurrentUser.GetOwnerId(ctx);
            if (ownerId == Guid.Empty) return Results.Unauthorized();
            var now = DateTimeOffset.UtcNow;
            var entity = new Spreadsheet
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                OwnerId = ownerId,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Spreadsheets.Add(entity);
            await db.SaveChangesAsync();
            var dto = new SpreadsheetDto(entity.Id, entity.Title, entity.CreatedAt, entity.UpdatedAt, "Owner");
            return Results.Created($"/api/spreadsheets/{entity.Id}", dto);
        });

        group.MapGet("/{id:guid}", async (Guid id, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(id, userId);
            if (level is null) return Results.NotFound();

            var entity = await db.Spreadsheets.FirstOrDefaultAsync(s => s.Id == id);
            return Results.Ok(new SpreadsheetDto(entity!.Id, entity.Title, entity.CreatedAt, entity.UpdatedAt, level.Value.ToString()));
        });

        group.MapDelete("/{id:guid}", async (Guid id, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(id, userId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.NotFound();

            var entity = await db.Spreadsheets.FirstOrDefaultAsync(s => s.Id == id);
            db.Spreadsheets.Remove(entity!);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
