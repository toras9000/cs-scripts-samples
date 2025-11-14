#r "nuget: Lestaly.General, 0.109.0"
#r "nuget: StreamJsonRpc, 2.22.23"
#load ".rcp-helper.csx"
#nullable enable
using System.Net;
using System.Net.Sockets;
using Lestaly;
using StreamJsonRpc;

var settings = new
{
    Service = new
    {
        Endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12233),
    },
};

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine($"Connect to {settings.Service.Endpoint}");
    using var tcp = new TcpClient();
    await tcp.ConnectAsync(settings.Service.Endpoint, signal.Token);

    WriteLine($"Start RPC");
    using var stream = tcp.GetStream();
    using var service = JsonRpc.Attach<IMemoryService>(stream);

    WriteLine($"Enter remote op");
    await InterpretCommandsAsync(service, signal.Token);

});
