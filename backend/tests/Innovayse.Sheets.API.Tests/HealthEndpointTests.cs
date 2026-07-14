using System.Net;
using System.Threading.Tasks;
using Xunit;

public class HealthEndpointTests : IClassFixture<SheetsApiFactory>
{
    private readonly SheetsApiFactory _factory;

    public HealthEndpointTests(SheetsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
