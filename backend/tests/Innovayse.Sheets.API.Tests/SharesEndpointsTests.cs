using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class SharesEndpointsTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public SharesEndpointsTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    private record SpreadsheetShape(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string AccessLevel);

    [Fact]
    public async Task AddShare_ThenList_ReturnsTheShare()
    {
        var client = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("viewer@example.com", TestAuthHandler.SecondTestOwnerId);

        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Shared Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();

        var addResponse = await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "viewer@example.com", role = "View" });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/shares");
        var shares = await listResponse.Content.ReadFromJsonAsync<List<ShareShape>>();
        Assert.Single(shares!);
        Assert.Equal(TestAuthHandler.SecondTestOwnerId, shares![0].UserId);
        Assert.Equal("View", shares[0].Role);
    }

    [Fact]
    public async Task AddShare_UnknownIdentifier_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();

        var addResponse = await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "nobody@example.com", role = "View" });
        Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
    }

    [Fact]
    public async Task AddShare_AsNonOwnerWithNoAccess_ReturnsNotFound()
    {
        var owner = _factory.CreateClient();
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();

        var stranger = _factory.CreateClient();
        stranger.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var response = await stranger.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "viewer@example.com", role = "View" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveShare_RemovesAccess()
    {
        var client = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("removeme@example.com", TestAuthHandler.SecondTestOwnerId);

        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "removeme@example.com", role = "Edit" });

        var deleteResponse = await client.DeleteAsync($"/api/spreadsheets/{spreadsheet.Id}/shares/{TestAuthHandler.SecondTestOwnerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/shares");
        var shares = await listResponse.Content.ReadFromJsonAsync<List<ShareShape>>();
        Assert.Empty(shares!);
    }

    private record ShareShape(Guid UserId, string Role);
}
