using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Innovayse.Sheets.API.Users;
using Xunit;

public class HttpSsoUserDirectoryTests
{
    private class FakeHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new HttpResponseMessage(HttpStatusCode.NotFound);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(ResponseToReturn);
        }
    }

    [Fact]
    public async Task ResolveUserId_calls_the_real_lookup_path_with_the_email_query_param()
    {
        var handler = new FakeHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"userId\":\"11111111-1111-1111-1111-111111111111\"}", System.Text.Encoding.UTF8, "application/json")
            }
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://sso.local") };
        var directory = new HttpSsoUserDirectory(httpClient);

        await directory.ResolveUserId("someone@example.com");

        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal("/api/service/users/lookup?email=someone%40example.com", handler.CapturedRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task ResolveUserId_sends_the_configured_service_key_header()
    {
        var handler = new FakeHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://sso.local") };
        httpClient.DefaultRequestHeaders.Add("X-Service-Key", "dev-service-key-sheets");
        var directory = new HttpSsoUserDirectory(httpClient);

        await directory.ResolveUserId("someone@example.com");

        Assert.True(handler.CapturedRequest!.Headers.TryGetValues("X-Service-Key", out var values));
        Assert.Equal("dev-service-key-sheets", System.Linq.Enumerable.Single(values));
    }

    [Fact]
    public async Task ResolveUserId_returns_the_userId_on_200()
    {
        var handler = new FakeHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"userId\":\"11111111-1111-1111-1111-111111111111\"}", System.Text.Encoding.UTF8, "application/json")
            }
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://sso.local") };
        var directory = new HttpSsoUserDirectory(httpClient);

        var result = await directory.ResolveUserId("someone@example.com");

        Assert.Equal(System.Guid.Parse("11111111-1111-1111-1111-111111111111"), result);
    }

    [Fact]
    public async Task ResolveUserId_returns_null_on_404()
    {
        var handler = new FakeHandler { ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NotFound) };
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://sso.local") };
        var directory = new HttpSsoUserDirectory(httpClient);

        var result = await directory.ResolveUserId("missing@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveUserId_returns_null_on_401()
    {
        var handler = new FakeHandler { ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Unauthorized) };
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://sso.local") };
        var directory = new HttpSsoUserDirectory(httpClient);

        var result = await directory.ResolveUserId("someone@example.com");

        Assert.Null(result);
    }
}
