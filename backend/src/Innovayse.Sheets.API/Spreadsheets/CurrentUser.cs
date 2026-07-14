using System.Security.Claims;

namespace Innovayse.Sheets.API.Spreadsheets;

internal static class CurrentUser
{
    public static Guid GetOwnerId(HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub");
        return sub is null ? Guid.Empty : Guid.Parse(sub);
    }
}
