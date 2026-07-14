using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Innovayse.Sheets.API.Spreadsheets;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Sharing;

public static class LinksEndpoints
{
    public static void MapLinksEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spreadsheets/{spreadsheetId:guid}/links").RequireAuthorization();

        group.MapGet("", async (Guid spreadsheetId, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.Forbid();

            var link = await db.Links.FirstOrDefaultAsync(l => l.SpreadsheetId == spreadsheetId && l.RevokedAt == null);
            if (link is null) return Results.NotFound();
            return Results.Ok(new LinkDto(link.Token, link.Role.ToString()));
        });

        group.MapPost("", async (Guid spreadsheetId, CreateLinkRequest request, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.Forbid();

            if (!Enum.TryParse<ShareRole>(request.Role, ignoreCase: true, out var role))
                return Results.BadRequest();

            var existing = await db.Links.Where(l => l.SpreadsheetId == spreadsheetId && l.RevokedAt == null).ToListAsync();
            foreach (var old in existing)
            {
                old.RevokedAt = DateTimeOffset.UtcNow;
            }

            var link = new SharingLink
            {
                Id = Guid.NewGuid(),
                SpreadsheetId = spreadsheetId,
                Token = Guid.NewGuid().ToString("N"),
                Role = role,
                CreatedByUserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Links.Add(link);
            await db.SaveChangesAsync();
            return Results.Created($"/api/spreadsheets/{spreadsheetId}/links", new LinkDto(link.Token, link.Role.ToString()));
        });

        group.MapDelete("", async (Guid spreadsheetId, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.Forbid();

            var link = await db.Links.FirstOrDefaultAsync(l => l.SpreadsheetId == spreadsheetId && l.RevokedAt == null);
            if (link is not null)
            {
                link.RevokedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });

        app.MapPost("/api/links/{token}/claim", async (string token, SheetsDbContext db, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var link = await db.Links.FirstOrDefaultAsync(l => l.Token == token && l.RevokedAt == null);
            if (link is null) return Results.NotFound();

            var existing = await db.Shares.FirstOrDefaultAsync(s => s.SpreadsheetId == link.SpreadsheetId && s.UserId == userId);
            if (existing is null)
            {
                db.Shares.Add(new SpreadsheetShare
                {
                    Id = Guid.NewGuid(),
                    SpreadsheetId = link.SpreadsheetId,
                    UserId = userId,
                    Role = link.Role,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else if (existing.Role == ShareRole.View && link.Role == ShareRole.Edit)
            {
                existing.Role = ShareRole.Edit;
            }
            // else: existing role is already Edit or equal — never downgrade.

            await db.SaveChangesAsync();
            return Results.Ok(new ClaimLinkResponse(link.SpreadsheetId));
        }).RequireAuthorization();
    }
}
