using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Innovayse.Sheets.API.Spreadsheets;
using Innovayse.Sheets.API.Users;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Sharing;

public static class SharesEndpoints
{
    public static void MapSharesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spreadsheets/{spreadsheetId:guid}/shares").RequireAuthorization();

        group.MapGet("", async (Guid spreadsheetId, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.Forbid();

            var shares = await db.Shares.Where(s => s.SpreadsheetId == spreadsheetId)
                .Select(s => new ShareDto(s.UserId, s.Role.ToString()))
                .ToListAsync();
            return Results.Ok(shares);
        });

        group.MapPost("", async (Guid spreadsheetId, CreateShareRequest request, SheetsDbContext db, ISpreadsheetAccessService access, ISsoUserDirectory directory, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.Forbid();

            if (!Enum.TryParse<ShareRole>(request.Role, ignoreCase: true, out var role))
                return Results.BadRequest();

            var targetUserId = await directory.ResolveUserId(request.UserIdentifier);
            if (targetUserId is null) return Results.NotFound();

            var existing = await db.Shares.FirstOrDefaultAsync(s => s.SpreadsheetId == spreadsheetId && s.UserId == targetUserId);
            if (existing is not null)
            {
                existing.Role = role;
            }
            else
            {
                db.Shares.Add(new SpreadsheetShare
                {
                    Id = Guid.NewGuid(),
                    SpreadsheetId = spreadsheetId,
                    UserId = targetUserId.Value,
                    Role = role,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            await db.SaveChangesAsync();
            return Results.Created($"/api/spreadsheets/{spreadsheetId}/shares/{targetUserId}", new ShareDto(targetUserId.Value, role.ToString()));
        });

        group.MapDelete("/{userId:guid}", async (Guid spreadsheetId, Guid userId, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var callerId = CurrentUser.GetOwnerId(ctx);
            if (callerId == Guid.Empty) return Results.Unauthorized();

            var level = await access.GetAccessLevel(spreadsheetId, callerId);
            if (level is null) return Results.NotFound();
            if (level != AccessLevel.Owner) return Results.Forbid();

            var share = await db.Shares.FirstOrDefaultAsync(s => s.SpreadsheetId == spreadsheetId && s.UserId == userId);
            if (share is not null)
            {
                db.Shares.Remove(share);
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });
    }
}
