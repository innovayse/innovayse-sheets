using System.Net.Http.Json;
using Innovayse.Sheets.API.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public class CellEndpointsRealtimeTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public CellEndpointsRealtimeTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    private record SpreadsheetShape(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string AccessLevel);
    private record SheetShape(Guid Id, string Name, int Order);
    private record CellShape(int Row, int Col, string RawValue, double? ComputedValue, string? TextValue, string? Error, string? FormatJson);

    [Fact]
    public async Task PatchCells_BroadcastsCellsUpdatedToJoinedConnections()
    {
        var client = _factory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Broadcast Test" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var sheet = await (await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" }))
            .Content.ReadFromJsonAsync<SheetShape>();

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(client.BaseAddress!, "/hubs/sheets"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers["X-Test-Owner-Id"] = TestAuthHandler.TestOwnerId.ToString();
            })
            .Build();

        var received = new List<CellShape[]>();
        connection.On<CellShape[]>("CellsUpdated", cells => received.Add(cells));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinSheet", sheet!.Id);

        await client.PatchAsJsonAsync($"/api/sheets/{sheet.Id}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "42", formatJson = (string?)null } }
        });

        await Task.Delay(200);

        Assert.Single(received);
        Assert.Equal(42, received[0][0].ComputedValue);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task PatchCells_StillReturnsOk_WhenBroadcastThrows()
    {
        // Regression test for the review finding: the broadcast call ran with no try/catch
        // before `return Results.Ok()`, so a SignalR failure would surface as a 500 even
        // though the cell write had already been committed. This uses a dedicated factory
        // instance with a fake IHubContext<SheetHub> whose group send always throws, proving
        // the endpoint's catch block isolates the write from the broadcast.
        await using var throwingFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHubContext<SheetHub>>();
                services.AddSingleton<IHubContext<SheetHub>>(new ThrowingHubContext());
            });
        });

        var client = throwingFactory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Broadcast Failure Test" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var sheet = await (await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" }))
            .Content.ReadFromJsonAsync<SheetShape>();

        var response = await client.PatchAsJsonAsync($"/api/sheets/{sheet!.Id}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "42", formatJson = (string?)null } }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var cellsResponse = await client.GetAsync($"/api/sheets/{sheet.Id}/cells");
        var cells = await cellsResponse.Content.ReadFromJsonAsync<CellShape[]>();
        Assert.Contains(cells!, c => c.Row == 0 && c.Col == 0 && c.RawValue == "42");
    }
}
