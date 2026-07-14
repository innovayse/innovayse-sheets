using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class SpreadsheetEndpointsTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public SpreadsheetEndpointsTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndGet_RoundTripsTitle()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Budget 2026" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SpreadsheetDtoShape>();

        var getResponse = await client.GetAsync($"/api/spreadsheets/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<SpreadsheetDtoShape>();

        Assert.Equal("Budget 2026", fetched!.Title);
    }

    [Fact]
    public async Task Delete_RemovesSpreadsheet()
    {
        var client = _factory.CreateClient();
        var created = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Temp" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();

        var deleteResponse = await client.DeleteAsync($"/api/spreadsheets/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/spreadsheets/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSheet_AddsSheetToSpreadsheet()
    {
        var client = _factory.CreateClient();
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Sheet Test" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();

        var sheetResponse = await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" });
        Assert.Equal(HttpStatusCode.Created, sheetResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/sheets");
        var sheets = await listResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<SheetDtoShape>>();
        Assert.Single(sheets!);
        Assert.Equal("Sheet1", sheets![0].Name);
    }

    [Fact]
    public async Task GetSheets_AsDifferentOwner_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        var spreadsheet = await (await ownerClient.PostAsJsonAsync("/api/spreadsheets", new { title = "Owner Only" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        await ownerClient.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" });

        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(TestAuthHandler.OwnerOverrideHeader, TestAuthHandler.SecondTestOwnerId.ToString());

        var response = await otherClient.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/sheets");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSheet_AsDifferentOwner_ReturnsNotFoundAndDoesNotCreate()
    {
        var ownerClient = _factory.CreateClient();
        var spreadsheet = await (await ownerClient.PostAsJsonAsync("/api/spreadsheets", new { title = "Owner Only 2" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();

        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(TestAuthHandler.OwnerOverrideHeader, TestAuthHandler.SecondTestOwnerId.ToString());

        var createResponse = await otherClient.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Intruder Sheet" });
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);

        var listResponse = await ownerClient.GetAsync($"/api/spreadsheets/{spreadsheet.Id}/sheets");
        var sheets = await listResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<SheetDtoShape>>();
        Assert.Empty(sheets!);
    }

    [Fact]
    public async Task List_IncludesSharedSpreadsheets()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("shareme@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Shared With Me" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "shareme@example.com", role = "Edit" });

        var viewer = _factory.CreateClient();
        viewer.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var listResponse = await viewer.GetAsync("/api/spreadsheets");
        var list = await listResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<SpreadsheetDtoShape>>();

        Assert.Contains(list!, s => s.Id == spreadsheet.Id && s.AccessLevel == "Edit");
    }

    [Fact]
    public async Task GetById_AsViewOnlyShare_CannotDelete()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("readonly@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "readonly@example.com", role = "View" });

        var viewer = _factory.CreateClient();
        viewer.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var getResponse = await viewer.GetAsync($"/api/spreadsheets/{spreadsheet.Id}");
        Assert.Equal(System.Net.HttpStatusCode.OK, getResponse.StatusCode);

        var deleteResponse = await viewer.DeleteAsync($"/api/spreadsheets/{spreadsheet.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Post_AsEditShare_CanCreateSheet()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("editor@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/shares", new { userIdentifier = "editor@example.com", role = "Edit" });

        var editor = _factory.CreateClient();
        editor.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var postResponse = await editor.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet.Id}/sheets", new { name = "Sheet1" });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
    }

    [Fact]
    public async Task GetSheetById_AsOwner_ReturnsSpreadsheetId()
    {
        var owner = _factory.CreateClient();
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Lookup Test" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        var sheetResponse = await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" });
        var sheet = await sheetResponse.Content.ReadFromJsonAsync<SheetDtoShape>();

        var lookupResponse = await owner.GetAsync($"/api/sheets/{sheet!.Id}");
        Assert.Equal(HttpStatusCode.OK, lookupResponse.StatusCode);
        var lookup = await lookupResponse.Content.ReadFromJsonAsync<SheetSummaryDtoShape>();

        Assert.Equal(sheet.Id, lookup!.Id);
        Assert.Equal(spreadsheet.Id, lookup.SpreadsheetId);
        Assert.Equal("Sheet1", lookup.Name);
    }

    [Fact]
    public async Task GetSheetById_AsStrangerWithNoAccess_ReturnsNotFound()
    {
        var owner = _factory.CreateClient();
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Private" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        var sheetResponse = await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" });
        var sheet = await sheetResponse.Content.ReadFromJsonAsync<SheetDtoShape>();

        var stranger = _factory.CreateClient();
        stranger.DefaultRequestHeaders.Add(TestAuthHandler.OwnerOverrideHeader, TestAuthHandler.SecondTestOwnerId.ToString());

        var lookupResponse = await stranger.GetAsync($"/api/sheets/{sheet!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, lookupResponse.StatusCode);
    }

    [Fact]
    public async Task GetSheetById_AsEditShare_ReturnsSpreadsheetId()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("sheetviewer@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Shared Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetDtoShape>();
        var sheetResponse = await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" });
        var sheet = await sheetResponse.Content.ReadFromJsonAsync<SheetDtoShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet.Id}/shares", new { userIdentifier = "sheetviewer@example.com", role = "Edit" });

        var editor = _factory.CreateClient();
        editor.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var lookupResponse = await editor.GetAsync($"/api/sheets/{sheet!.Id}");
        Assert.Equal(HttpStatusCode.OK, lookupResponse.StatusCode);
        var lookup = await lookupResponse.Content.ReadFromJsonAsync<SheetSummaryDtoShape>();
        Assert.Equal(spreadsheet.Id, lookup!.SpreadsheetId);
    }

    private record SpreadsheetDtoShape(System.Guid Id, string Title, System.DateTimeOffset CreatedAt, System.DateTimeOffset UpdatedAt, string AccessLevel);
    private record SheetDtoShape(System.Guid Id, string Name, int Order);
    private record SheetSummaryDtoShape(System.Guid Id, System.Guid SpreadsheetId, string Name, int Order);
}
