using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class SheetHubTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public SheetHubTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    private record SpreadsheetShape(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string AccessLevel);
    private record SheetShape(Guid Id, string Name, int Order);

    private HubConnection BuildConnection(string ownerIdHeaderValue)
    {
        var httpClient = _factory.CreateClient();
        return new HubConnectionBuilder()
            .WithUrl(new Uri(httpClient.BaseAddress!, "/hubs/sheets"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers["X-Test-Owner-Id"] = ownerIdHeaderValue;
            })
            .Build();
    }

    private async Task<(Guid spreadsheetId, Guid sheetId)> CreateSpreadsheetAndSheetAsync()
    {
        var client = _factory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Realtime Test" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var sheet = await (await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" }))
            .Content.ReadFromJsonAsync<SheetShape>();
        return (spreadsheet.Id, sheet!.Id);
    }

    [Fact]
    public async Task JoinSheet_AsOwner_Succeeds()
    {
        var (_, sheetId) = await CreateSpreadsheetAndSheetAsync();
        var connection = BuildConnection(TestAuthHandler.TestOwnerId.ToString());

        await connection.StartAsync();
        await connection.InvokeAsync("JoinSheet", sheetId);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task JoinSheet_AsStrangerWithNoAccess_Throws()
    {
        var (_, sheetId) = await CreateSpreadsheetAndSheetAsync();
        var connection = BuildConnection(TestAuthHandler.SecondTestOwnerId.ToString());

        await connection.StartAsync();
        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("JoinSheet", sheetId));

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task JoinSheet_ThenSecondUserJoins_BothReceivePresenceUpdate()
    {
        var (spreadsheetId, sheetId) = await CreateSpreadsheetAndSheetAsync();

        // Grant the second identity View access so its join succeeds.
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("viewer@example.com", TestAuthHandler.SecondTestOwnerId);
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheetId}/shares", new { userIdentifier = "viewer@example.com", role = "View" });

        var firstPresence = new List<string[]>();
        var connectionA = BuildConnection(TestAuthHandler.TestOwnerId.ToString());
        connectionA.On<string[]>("Presence", users => firstPresence.Add(users));
        await connectionA.StartAsync();
        await connectionA.InvokeAsync("JoinSheet", sheetId);

        var connectionB = BuildConnection(TestAuthHandler.SecondTestOwnerId.ToString());
        await connectionB.StartAsync();
        await connectionB.InvokeAsync("JoinSheet", sheetId);

        // Give the broadcast a brief moment to arrive at connectionA.
        await Task.Delay(200);

        Assert.Contains(firstPresence, users => users.Length == 2);

        await connectionA.DisposeAsync();
        await connectionB.DisposeAsync();
    }
}
