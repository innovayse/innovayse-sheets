using Innovayse.Sheets.API.Access;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class SpreadsheetAccessServiceTests
{
    private static SheetsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SheetsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SheetsDbContext(options);
    }

    [Fact]
    public async Task GetAccessLevel_Owner_ReturnsOwner()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var spreadsheetId = Guid.NewGuid();
        db.Spreadsheets.Add(new Spreadsheet { Id = spreadsheetId, OwnerId = ownerId, Title = "T", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new SpreadsheetAccessService(db);
        var level = await service.GetAccessLevel(spreadsheetId, ownerId);

        Assert.Equal(AccessLevel.Owner, level);
    }

    [Fact]
    public async Task GetAccessLevel_ExplicitEditShare_ReturnsEdit()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var spreadsheetId = Guid.NewGuid();
        db.Spreadsheets.Add(new Spreadsheet { Id = spreadsheetId, OwnerId = ownerId, Title = "T", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        db.Shares.Add(new SpreadsheetShare { Id = Guid.NewGuid(), SpreadsheetId = spreadsheetId, UserId = viewerId, Role = ShareRole.Edit, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new SpreadsheetAccessService(db);
        var level = await service.GetAccessLevel(spreadsheetId, viewerId);

        Assert.Equal(AccessLevel.Edit, level);
    }

    [Fact]
    public async Task GetAccessLevel_ExplicitViewShare_ReturnsView()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var spreadsheetId = Guid.NewGuid();
        db.Spreadsheets.Add(new Spreadsheet { Id = spreadsheetId, OwnerId = ownerId, Title = "T", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        db.Shares.Add(new SpreadsheetShare { Id = Guid.NewGuid(), SpreadsheetId = spreadsheetId, UserId = viewerId, Role = ShareRole.View, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new SpreadsheetAccessService(db);
        var level = await service.GetAccessLevel(spreadsheetId, viewerId);

        Assert.Equal(AccessLevel.View, level);
    }

    [Fact]
    public async Task GetAccessLevel_NoShareNoOwnership_ReturnsNull()
    {
        await using var db = CreateContext();
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var spreadsheetId = Guid.NewGuid();
        db.Spreadsheets.Add(new Spreadsheet { Id = spreadsheetId, OwnerId = ownerId, Title = "T", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new SpreadsheetAccessService(db);
        var level = await service.GetAccessLevel(spreadsheetId, strangerId);

        Assert.Null(level);
    }

    [Fact]
    public async Task GetAccessLevel_UnknownSpreadsheet_ReturnsNull()
    {
        await using var db = CreateContext();
        var service = new SpreadsheetAccessService(db);

        var level = await service.GetAccessLevel(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(level);
    }
}
