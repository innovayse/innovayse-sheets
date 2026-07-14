using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Innovayse.Sheets.API.Realtime;
using Innovayse.Sheets.API.Spreadsheets;
using Innovayse.Sheets.Formulas;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Cells;

public static class CellEndpoints
{
    public static void MapCellEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sheets/{sheetId:guid}/cells").RequireAuthorization();

        group.MapGet("", async (Guid sheetId, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var sheet = await db.Sheets.FirstOrDefaultAsync(sh => sh.Id == sheetId);
            if (sheet is null) return Results.NotFound();

            var level = await access.GetAccessLevel(sheet.SpreadsheetId, userId);
            if (level is null) return Results.NotFound();

            var cells = await db.Cells.Where(c => c.SheetId == sheetId).ToListAsync();
            var rawValues = cells.ToDictionary(c => new CellAddress(c.Row, c.Col), c => c.RawValue);
            var lookup = new DbCellValueLookup(rawValues);

            var dtos = cells.Select(c =>
            {
                var address = new CellAddress(c.Row, c.Col);
                var isFormulaOrNumeric = c.RawValue.StartsWith('=') || double.TryParse(c.RawValue, out _);
                if (!isFormulaOrNumeric)
                {
                    return new CellDto(c.Row, c.Col, c.RawValue, null, c.RawValue, null, c.FormatJson);
                }

                var result = lookup.GetValue(address);
                return result.IsError
                    ? new CellDto(c.Row, c.Col, c.RawValue, null, null, result.Error.Display, c.FormatJson)
                    : new CellDto(c.Row, c.Col, c.RawValue, result.Value, null, null, c.FormatJson);
            }).ToList();

            return Results.Ok(dtos);
        });

        group.MapPatch("", async (Guid sheetId, BatchCellWriteRequest request, SheetsDbContext db, ISpreadsheetAccessService access, HttpContext ctx, ILogger<Program> logger, IHubContext<SheetHub> hub) =>
        {
            var userId = CurrentUser.GetOwnerId(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var sheet = await db.Sheets.FirstOrDefaultAsync(sh => sh.Id == sheetId);
            if (sheet is null) return Results.NotFound();

            var level = await access.GetAccessLevel(sheet.SpreadsheetId, userId);
            if (level is null) return Results.NotFound();
            if (level == AccessLevel.View) return Results.NotFound();

            var existing = await db.Cells.Where(c => c.SheetId == sheetId).ToListAsync();
            var byAddress = existing.ToDictionary(c => new CellAddress(c.Row, c.Col));

            foreach (var write in request.Cells)
            {
                var address = new CellAddress(write.Row, write.Col);
                if (byAddress.TryGetValue(address, out var entity))
                {
                    entity.RawValue = write.RawValue;
                    if (write.FormatJson is not null)
                    {
                        entity.FormatJson = write.FormatJson;
                    }
                }
                else
                {
                    var entity2 = new Cell
                    {
                        Id = Guid.NewGuid(),
                        SheetId = sheetId,
                        Row = write.Row,
                        Col = write.Col,
                        RawValue = write.RawValue,
                        FormatJson = write.FormatJson
                    };
                    db.Cells.Add(entity2);
                    byAddress[address] = entity2;
                }
            }

            // Finding 5: exercise DependencyGraph cycle detection over the post-write formula
            // graph for this sheet. This is informational (logged) only — circular formulas must
            // remain a valid save; the #CIRCULAR! value is still produced at read time by
            // DbCellValueLookup's own evaluating-set detection, which is left untouched.
            try
            {
                var graph = new DependencyGraph();
                foreach (var c in byAddress.Values)
                {
                    if (c.RawValue.StartsWith('='))
                    {
                        var address = new CellAddress(c.Row, c.Col);
                        var ast = Parser.Parse(c.RawValue[1..]);
                        graph.AddCell(address, ast);
                    }
                }
                foreach (var write in request.Cells)
                {
                    var address = new CellAddress(write.Row, write.Col);
                    if (graph.HasCycle(address))
                    {
                        logger.LogInformation("Cell {SheetId}/{Row}/{Col} is part of a circular formula reference", sheetId, write.Row, write.Col);
                    }
                }
            }
            catch (Exception ex)
            {
                // Cycle detection is informational only; a malformed formula must never block a save
                // (writes always succeed — #VALUE!/#CIRCULAR! surface at read time instead).
                logger.LogWarning(ex, "DependencyGraph analysis failed for sheet {SheetId}; write proceeds regardless", sheetId);
            }

            await db.SaveChangesAsync();

            var rawValuesForBroadcast = byAddress.Values.ToDictionary(c => new CellAddress(c.Row, c.Col), c => c.RawValue);
            var broadcastLookup = new DbCellValueLookup(rawValuesForBroadcast);
            var updatedDtos = request.Cells.Select(write =>
            {
                var address = new CellAddress(write.Row, write.Col);
                var entity = byAddress[address];
                var isFormulaOrNumeric = entity.RawValue.StartsWith('=') || double.TryParse(entity.RawValue, out _);
                if (!isFormulaOrNumeric)
                {
                    return new CellDto(entity.Row, entity.Col, entity.RawValue, null, entity.RawValue, null, entity.FormatJson);
                }
                var result = broadcastLookup.GetValue(address);
                return result.IsError
                    ? new CellDto(entity.Row, entity.Col, entity.RawValue, null, null, result.Error.Display, entity.FormatJson)
                    : new CellDto(entity.Row, entity.Col, entity.RawValue, result.Value, null, null, entity.FormatJson);
            }).ToList();

            try
            {
                await hub.Clients.Group(sheetId.ToString()).SendAsync("CellsUpdated", updatedDtos);
            }
            catch (Exception ex)
            {
                // Broadcasting is a non-critical side effect; the write has already been
                // committed via SaveChangesAsync above, so a SignalR failure must never
                // turn a successful save into a 500 response.
                logger.LogWarning(ex, "CellsUpdated broadcast failed for sheet {SheetId}; write already succeeded", sheetId);
            }

            return Results.Ok();
        });
    }
}
