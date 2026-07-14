using System.Net.Http.Json;

namespace Innovayse.Sheets.API.Users;

public class HttpSsoUserDirectory : ISsoUserDirectory
{
    private readonly HttpClient _httpClient;

    public HttpSsoUserDirectory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid?> ResolveUserId(string identifier)
    {
        var response = await _httpClient.GetAsync($"/api/service/users/lookup?email={Uri.EscapeDataString(identifier)}");
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<UserLookupResponse>();
        return result?.UserId;
    }

    private record UserLookupResponse(Guid UserId);
}
