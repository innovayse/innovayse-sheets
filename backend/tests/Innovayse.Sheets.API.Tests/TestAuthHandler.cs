using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static readonly Guid TestOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SecondTestOwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string OwnerOverrideHeader = "X-Test-Owner-Id";
    public const string NoOwnerClaimHeader = "X-Test-No-Owner-Claim";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(NoOwnerClaimHeader))
        {
            var anonIdentity = new ClaimsIdentity(Array.Empty<Claim>(), "Test");
            var anonPrincipal = new ClaimsPrincipal(anonIdentity);
            var anonTicket = new AuthenticationTicket(anonPrincipal, "Test");
            return Task.FromResult(AuthenticateResult.Success(anonTicket));
        }

        var ownerId = TestOwnerId;
        if (Request.Headers.TryGetValue(OwnerOverrideHeader, out var overrideValue) &&
            Guid.TryParse(overrideValue, out var parsed))
        {
            ownerId = parsed;
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, ownerId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
