using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SegoAndBux.Tests.Fakes
{
    /// <summary>Minimal IHubContext stub that records SendAsync calls.</summary>
    public class FakeHubContext<THub> : IHubContext<THub> where THub : Hub
    {
        public FakeHubClients ClientsImpl { get; } = new();
        public IHubClients Clients => ClientsImpl;
        public IGroupManager Groups { get; } = new FakeGroupManager();

        public class FakeHubClients : IHubClients
        {
            // Everything sent to "All" (and friends) ends up here:
            public List<(string Method, object?[] Args)> AllCalls { get; } = new();

            private readonly FakeClientProxy _proxy;
            public FakeHubClients() => _proxy = new FakeClientProxy(AllCalls);

            public IClientProxy All => _proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> _) => _proxy;
            public IClientProxy Client(string _) => _proxy;
            public IClientProxy Clients(IReadOnlyList<string> _) => _proxy;
            public IClientProxy Group(string _) => _proxy;
            public IClientProxy GroupExcept(string _, IReadOnlyList<string> __) => _proxy;
            public IClientProxy Groups(IReadOnlyList<string> _) => _proxy;
            public IClientProxy User(string _) => _proxy;
            public IClientProxy Users(IReadOnlyList<string> _) => _proxy;

            private class FakeClientProxy : IClientProxy
            {
                private readonly List<(string Method, object?[] Args)> _sink;
                public FakeClientProxy(List<(string Method, object?[] Args)> sink) => _sink = sink;

                public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
                {
                    _sink.Add((method, args));
                    return Task.CompletedTask;
                }
            }
        }

        private class FakeGroupManager : IGroupManager
        {
            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
