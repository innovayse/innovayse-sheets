namespace Innovayse.Sheets.API.Users;

public interface ISsoUserDirectory
{
    Task<Guid?> ResolveUserId(string identifier);
}
