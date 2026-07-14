using System.Net;
using System.Net.Http.Json;
using Innovayse.Sheets.API.Data;
using Innovayse.Sheets.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class LinksEndpointsTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public LinksEndpointsTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    private record SpreadsheetShape(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string AccessLevel);
    private record LinkShape(string Token, string Role);
    private record ClaimResponseShape(Guid SpreadsheetId);

    [Fact]
    public async Task CreateLink_ThenGet_ReturnsSameLink()
    {
        var client = _factory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();

        var createResponse = await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/links", new { role = "View" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<LinkShape>();

        var getResponse = await client.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/links");
        var fetched = await getResponse.Content.ReadFromJsonAsync<LinkShape>();

        Assert.Equal(created!.Token, fetched!.Token);
        Assert.Equal("View", fetched.Role);
    }

    [Fact]
    public async Task CreateLink_Twice_RevokesThePriorOne()
    {
        var client = _factory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();

        var first = await (await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/links", new { role = "View" }))
            .Content.ReadFromJsonAsync<LinkShape>();
        var second = await (await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet.Id}/links", new { role = "Edit" }))
            .Content.ReadFromJsonAsync<LinkShape>();

        var claimOldResponse = await client.PostAsJsonAsync($"/api/links/{first!.Token}/claim", new { });
        Assert.Equal(HttpStatusCode.NotFound, claimOldResponse.StatusCode);

        var claimNewResponse = await client.PostAsJsonAsync($"/api/links/{second!.Token}/claim", new { });
        Assert.Equal(HttpStatusCode.OK, claimNewResponse.StatusCode);
    }

    [Fact]
    public async Task ClaimLink_GrantsAccessAtLinkRole()
    {
        var owner = _factory.CreateClient();
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var link = await (await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/links", new { role = "Edit" }))
            .Content.ReadFromJsonAsync<LinkShape>();

        var claimer = _factory.CreateClient();
        claimer.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var claimResponse = await claimer.PostAsJsonAsync($"/api/links/{link!.Token}/claim", new { });
        Assert.Equal(HttpStatusCode.OK, claimResponse.StatusCode);
        var claimed = await claimResponse.Content.ReadFromJsonAsync<ClaimResponseShape>();
        Assert.Equal(spreadsheet.Id, claimed!.SpreadsheetId);

        // Claim creates a SpreadsheetShare row at the link's role
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SheetsDbContext>();
        var share = await db.Shares.SingleAsync(s => s.SpreadsheetId == spreadsheet.Id && s.UserId == TestAuthHandler.SecondTestOwnerId);
        Assert.Equal(ShareRole.Edit, share.Role);
    }

    [Fact]
    public async Task ClaimLink_DoesNotDowngradeExistingHigherRole()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("editor@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "editor@example.com", role = "Edit" });
        var link = await (await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet.Id}/links", new { role = "View" }))
            .Content.ReadFromJsonAsync<LinkShape>();

        var claimer = _factory.CreateClient();
        claimer.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());
        await claimer.PostAsJsonAsync($"/api/links/{link!.Token}/claim", new { });

        var sharesResponse = await owner.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/shares");
        var shares = await sharesResponse.Content.ReadFromJsonAsync<List<ShareShapeForDowngradeTest>>();
        Assert.Equal("Edit", shares!.Single(s => s.UserId == TestAuthHandler.SecondTestOwnerId).Role);
    }

    [Fact]
    public async Task ClaimUnknownToken_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/links/does-not-exist/claim", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record ShareShapeForDowngradeTest(Guid UserId, string Role);
}
