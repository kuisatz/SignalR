using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class BroadcastBenchmark
    {
        private DefaultHubLifetimeManager<Hub> _hubLifetimeManager;
        private HubContext<Hub> _hubContext;

        [Params(1, 10, 1000)]
        public int Connections;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _hubLifetimeManager = new DefaultHubLifetimeManager<Hub>();
            var options = new UnboundedChannelOptions { AllowSynchronousContinuations = true };

            for (var i = 0; i < Connections; ++i)
            {
                var transportToApplication = Channel.CreateUnbounded<byte[]>(options);
                var applicationToTransport = Channel.CreateUnbounded<byte[]>(options);

                var application = ChannelConnection.Create<byte[]>(input: applicationToTransport, output: transportToApplication);
                var transport = ChannelConnection.Create<byte[]>(input: transportToApplication, output: applicationToTransport);
                var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), transport, application);

                _hubLifetimeManager.OnConnectedAsync(new HubConnectionContext(connection, Timeout.InfiniteTimeSpan, NullLoggerFactory.Instance)).Wait();
            }

            _hubContext = new HubContext<Hub>(_hubLifetimeManager);
        }

        [Benchmark]
        public Task SendAsyncAll()
        {
            return _hubContext.Clients.All.SendAsync("Method");
        }
    }
}
