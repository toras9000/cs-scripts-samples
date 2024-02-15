#load ".tcp-helper.csx"
#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Kokuban;
using Lestaly;

var settings = new
{
    Server = new
    {
        Interface = IPAddress.Parse("127.0.0.1"),
        Port = 9971,
    },
};

return await Paved.RunAsync(async () =>
{
    // Preparation for Console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Indicate the status.
    var endpoint = new IPEndPoint(settings.Server.Interface, settings.Server.Port);
    Console.WriteLine($"Listen on {endpoint}");

    while (true)
    {
        // Waiting for connection.
        Console.WriteLine($"Wait for connection ...");
        var tcp = default(TcpClient);
        do
        {
            using var listener = new TcpListener(endpoint);
            listener.Start(1);
            tcp = await listener.AcceptTcpClientAsync(signal.Token);
        } while (false);

        // Display the connection source.
        Console.WriteLine($"Connection was accepted from {tcp.Client.RemoteEndPoint}");

        // Generates a token source that aborts processing for the current connection.
        var braker = CancellationTokenSource.CreateLinkedTokenSource(signal.Token);

        // Performs asynchronous processing to dump out received data.
        var receiver = Task.Run(async () => await TcpReceiveDumper(tcp, braker.Token));

        // Loop to send the entered text
        while (!braker.Token.IsCancellationRequested)
        {
            // Waiting for input
            ConsoleUtils.WritePrompt();
            var line = await ConsoleWig.InReader.ReadLineAsync(braker.Token);
            if (line.IsEmpty()) continue;

            // To make it into a byte sequence with some encoding.
            var bytes = Encoding.UTF8.GetBytes(line);

            // Sending. A SocketException is considered a connection not being kept.
            try { await tcp.Client.SendAsync(bytes, braker.Token); }
            catch (SocketException) { break; }
        }

        // Indicate the status.
        ConsoleUtils.WriteAttention("Connection closed");

        // Finish the receiving dump process.
        braker.Cancel();
        try { await receiver; }
        catch (OperationCanceledException) when (!signal.Token.IsCancellationRequested) { }
    }
});
