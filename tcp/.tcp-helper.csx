#r "nuget: Lestaly.General, 0.112.0"
#load ".console-utils.csx"
#nullable enable
using System.Net.Sockets;
using System.Threading;

async Task TcpReceiveDumper(TcpClient tcp, CancellationToken cancelToken)
{
    var buffer = new byte[8192];
    while (true)
    {
        try
        {
            var length = await tcp.Client.ReceiveAsync(buffer, cancelToken);
            if (length == 0) break;

            var text = Encoding.ASCII.GetString(buffer.AsSpan(0, length));
            ConsoleUtils.WriteInsertion(text);
        }
        catch (SocketException)
        {
        }
    }
}
