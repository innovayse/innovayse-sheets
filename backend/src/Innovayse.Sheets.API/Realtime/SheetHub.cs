using System.Collections.Concurrent;
using System.Security.Claims;
using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Sheets.API.Realtime;

public class SheetHub : Hub
{
    // sheetId -> set of (connectionId -> userId), so OnDisconnectedAsync can find which
    // sheet group(s) a dropped connection belonged to and recompute presence.
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, Guid>> SheetConnections = new();

    private readonly SheetsDbContext _db;
    private readonly ISpreadsheetAccessService _access;

    public SheetHub(SheetsDbContext db, ISpreadsheetAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task JoinSheet(Guid sheetId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            throw new HubException("Unauthorized");
        }

        var sheet = await _db.Sheets.FirstOrDefaultAsync(sh => sh.Id == sheetId);
        if (sheet is null)
        {
            throw new HubException("Not found");
        }

        var level = await _access.GetAccessLevel(sheet.SpreadsheetId, userId);
        if (level is null)
        {
            throw new HubException("Not found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sheetId.ToString());

        var connections = SheetConnections.GetOrAdd(sheetId, _ => new ConcurrentDictionary<string, Guid>());
        connections[Context.ConnectionId] = userId;

        await BroadcastPresence(sheetId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var (sheetId, connections) in SheetConnections)
        {
            if (connections.TryRemove(Context.ConnectionId, out _))
            {
                await BroadcastPresence(sheetId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task BroadcastPresence(Guid sheetId)
    {
        if (!SheetConnections.TryGetValue(sheetId, out var connections))
        {
            return;
        }

        var userIds = connections.Values.Distinct().Select(id => id.ToString()).ToArray();
        await Clients.Group(sheetId.ToString()).SendAsync("Presence", userIds);
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
        return sub is null ? Guid.Empty : Guid.Parse(sub);
    }
}
