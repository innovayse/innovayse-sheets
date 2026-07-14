using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class CellEndpointsTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public CellEndpointsTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<System.Guid> CreateSheetAsync(System.Net.Http.HttpClient client)
    {
        var spreadsheet = await (await client.PostAsJsonAsync("/api/spreadsheets", new { title = "Cells Test" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var sheet = await (await client.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" }))
            .Content.ReadFromJsonAsync<SheetShape>();
        return sheet!.Id;
    }

    [Fact]
    public async Task BatchWrite_ThenRead_ReturnsLiteralValues()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        var writeResponse = await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "5", formatJson = (string?)null } }
        });
        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);

        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");
        var cells = await readResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<CellShape>>();

        Assert.Single(cells!);
        Assert.Equal(5, cells![0].ComputedValue);
    }

    [Fact]
    public async Task BatchWrite_FormulaReferencingAnotherCell_ComputesValue()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[]
            {
                new { row = 0, col = 0, rawValue = "5", formatJson = (string?)null },
                new { row = 0, col = 1, rawValue = "=A1+1", formatJson = (string?)null }
            }
        });

        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");
        var cells = await readResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<CellShape>>();

        var b1 = cells!.Find(c => c.Row == 0 && c.Col == 1);
        Assert.Equal(6, b1!.ComputedValue);
    }

    [Fact]
    public async Task BatchWrite_DivideByZeroFormula_ReturnsErrorCode()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "=10/0", formatJson = (string?)null } }
        });

        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");
        var cells = await readResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<CellShape>>();

        Assert.Equal("#DIV/0!", cells![0].Error);
        Assert.Null(cells[0].ComputedValue);
    }

    [Fact]
    public async Task BatchWrite_CircularFormula_ReturnsCircularError()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[]
            {
                new { row = 0, col = 0, rawValue = "=B1", formatJson = (string?)null },
                new { row = 0, col = 1, rawValue = "=A1", formatJson = (string?)null }
            }
        });

        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");
        var cells = await readResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<CellShape>>();

        Assert.Contains(cells!, c => c.Error == "#CIRCULAR!");
    }

    [Fact]
    public async Task BatchWrite_TextValue_ReturnsTextValueChannel()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "Name", formatJson = (string?)null } }
        });

        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");
        var cells = await readResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<CellShape>>();

        Assert.Single(cells!);
        Assert.Equal("Name", cells![0].TextValue);
        Assert.Null(cells[0].ComputedValue);
        Assert.Null(cells[0].Error);
    }

    [Fact]
    public async Task BatchWrite_SecondWriteWithNullFormatJson_PreservesExistingFormat()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "5", formatJson = "{\"bold\":true}" } }
        });

        await client.PatchAsJsonAsync($"/api/sheets/{sheetId}/cells", new
        {
            cells = new[] { new { row = 0, col = 0, rawValue = "6", formatJson = (string?)null } }
        });

        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");
        var cells = await readResponse.Content.ReadFromJsonAsync<System.Collections.Generic.List<CellShape>>();

        Assert.Single(cells!);
        Assert.Equal("{\"bold\":true}", cells![0].FormatJson);
        Assert.Equal(6, cells[0].ComputedValue);
    }

    [Fact]
    public async Task NoOwnerClaim_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var sheetId = await CreateSheetAsync(client);

        client.DefaultRequestHeaders.Add(TestAuthHandler.NoOwnerClaimHeader, "true");
        var readResponse = await client.GetAsync($"/api/sheets/{sheetId}/cells");

        Assert.Equal(HttpStatusCode.Unauthorized, readResponse.StatusCode);
    }

    [Fact]
    public async Task PatchCells_AsViewOnlyShare_ReturnsNotFound()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("viewonly@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var sheet = await (await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" }))
            .Content.ReadFromJsonAsync<SheetShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet.Id}/shares", new { userIdentifier = "viewonly@example.com", role = "View" });

        var viewer = _factory.CreateClient();
        viewer.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var getResponse = await viewer.GetAsync($"/api/sheets/{sheet!.Id}/cells");
        Assert.Equal(System.Net.HttpStatusCode.OK, getResponse.StatusCode);

        var patchResponse = await viewer.PatchAsJsonAsync($"/api/sheets/{sheet.Id}/cells", new { cells = new[] { new { row = 0, col = 0, rawValue = "5", formatJson = (string?)null } } });
        Assert.Equal(System.Net.HttpStatusCode.NotFound, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchCells_AsEditShare_CanWrite()
    {
        var owner = _factory.CreateClient();
        _factory.Services.GetRequiredService<FakeSsoUserDirectory>().Register("editor2@example.com", TestAuthHandler.SecondTestOwnerId);
        var spreadsheet = await (await owner.PostAsJsonAsync("/api/spreadsheets", new { title = "Doc" }))
            .Content.ReadFromJsonAsync<SpreadsheetShape>();
        var sheet = await (await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet!.Id}/sheets", new { name = "Sheet1" }))
            .Content.ReadFromJsonAsync<SheetShape>();
        await owner.PostAsJsonAsync($"/api/spreadsheets/{spreadsheet.Id}/shares", new { userIdentifier = "editor2@example.com", role = "Edit" });

        var editor = _factory.CreateClient();
        editor.DefaultRequestHeaders.Add("X-Test-Owner-Id", TestAuthHandler.SecondTestOwnerId.ToString());

        var patchResponse = await editor.PatchAsJsonAsync($"/api/sheets/{sheet!.Id}/cells", new { cells = new[] { new { row = 0, col = 0, rawValue = "5", formatJson = (string?)null } } });
        Assert.Equal(System.Net.HttpStatusCode.OK, patchResponse.StatusCode);
    }

    private record SpreadsheetShape(System.Guid Id, string Title, System.DateTimeOffset CreatedAt, System.DateTimeOffset UpdatedAt, string AccessLevel);
    private record SheetShape(System.Guid Id, string Name, int Order);
    private record CellShape(int Row, int Col, string RawValue, double? ComputedValue, string? TextValue, string? Error, string? FormatJson);
}
