#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: StreamJsonRpc, 2.22.11"
#load ".rcp-helper.csx"
#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Kokuban;
using Lestaly;
using StreamJsonRpc;

var settings = new
{
    Service = new
    {
        Endpoint = new IPEndPoint(IPAddress.Any, 12233),
    },
};

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    var memory = new MemoryServer();

    while (true)
    {
        try
        {
            WriteLine($"Listen client : Endpoint={settings.Service.Endpoint}");
            using var tcp = await acceptTcpAsync(settings.Service.Endpoint, signal.Token);
            WriteLine($"Accepted from {tcp.Client.RemoteEndPoint}");

            WriteLine($"Start RPC");
            using var stream = tcp.GetStream();
            using var service = JsonRpc.Attach(stream, memory);
            using var breaker = CancellationTokenSource.CreateLinkedTokenSource(signal.Token);
            service.Disconnected += (s, a) => breaker.Cancel();

            WriteLine($"Enter local op");
            try
            {
                await InterpretCommandsAsync(memory, breaker.Token);
            }
            catch (OperationCanceledException) when (!signal.Token.IsCancellationRequested)
            {
                // disconnected
            }
        }
        catch (Exception ex) when (!signal.Token.IsCancellationRequested)
        {
            WriteLine(Chalk.Yellow[ex.Message]);
        }
    }
});

static async Task<TcpClient> acceptTcpAsync(IPEndPoint endpoint, CancellationToken cancelToken)
{
    using var listener = new TcpListener(endpoint);
    listener.Start();
    return await listener.AcceptTcpClientAsync();
}

public class MemoryServer : IMemoryService
{
    public Task<KeyValuePair<string, string>[]> GetListAsync()
    {
        lock (this.data)
        {
            return Task.FromResult(this.data.ToArray());
        }
    }

    public Task<string?> GetEntryAsync(string key)
    {
        lock (this.data)
        {
            return Task.FromResult(this.data.GetValueOrDefault(key));
        }
    }

    public Task<bool> SetEntryAsync(string key, string? value)
    {
        lock (this.data)
        {
            var result = default(bool);
            if (value == null)
            {
                result = this.data.Remove(key);
            }
            else
            {
                result = this.data.ContainsKey(key);
                this.data[key] = value;
            }
            return Task.FromResult(result);
        }
    }

    public void Dispose() { }

    private Dictionary<string, string> data = new();
}
