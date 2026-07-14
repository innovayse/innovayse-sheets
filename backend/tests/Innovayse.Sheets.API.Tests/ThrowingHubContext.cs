using Innovayse.Sheets.API.Realtime;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// A minimal fake IHubContext&lt;SheetHub&gt; whose group broadcast always throws, used to prove
/// that a SignalR broadcast failure never fails the PATCH /cells write it follows. There is no
/// mocking library in this test project, so the fake implements just enough of the interface
/// surface the endpoint touches (Clients.Group(...).SendAsync(...)).
/// </summary>
public class ThrowingHubContext : IHubContext<SheetHub>
{
    public IHubClients Clients { get; } = new ThrowingHubClients();
    public IGroupManager Groups => throw new NotSupportedException("Not used by CellEndpoints");

    private class ThrowingHubClients : IHubClients
    {
        public IClientProxy All => throw new NotSupportedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy Group(string groupName) => new ThrowingClientProxy();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private class ThrowingClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated SignalR broadcast failure");
        }
    }
}
