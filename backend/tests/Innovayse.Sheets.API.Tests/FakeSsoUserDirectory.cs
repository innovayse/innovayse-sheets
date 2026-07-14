using Innovayse.Sheets.API.Users;

public class FakeSsoUserDirectory : ISsoUserDirectory
{
    private readonly Dictionary<string, Guid> _users = new();

    public FakeSsoUserDirectory Register(string identifier, Guid userId)
    {
        _users[identifier] = userId;
        return this;
    }

    public Task<Guid?> ResolveUserId(string identifier) =>
        Task.FromResult(_users.TryGetValue(identifier, out var id) ? id : (Guid?)null);
}
