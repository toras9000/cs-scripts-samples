#r "nuget: MQTTnet, 4.3.3.952"
#r "nuget: Lestaly, 0.56.0"
#nullable enable
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Lestaly;
using MQTTnet;
using MQTTnet.Client;

var settings = new
{
    BrokerHost = "test.mosquitto.org",

    BrokerPort = 1883,

    ClientId = $"test-cid-{Guid.NewGuid()}",

    PublishTopic = "mytest/test/pub",

    SubscribeTopic = "mytest/test/+",
};

return await Paved.RunAsync(async () =>
{
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Client optinos
    var clientOptinos = new MqttClientOptionsBuilder()
        .WithTcpServer(settings.BrokerHost, settings.BrokerPort)
        .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
        .WithCleanSession(true)
        .WithWillRetain(false)
        .WithClientId(settings.ClientId)
        .Build();

    // Context information
    ConsoleWig.WriteLine("Context information");
    ConsoleWig.WriteLine($"  Broker   : {settings.BrokerHost}:{settings.BrokerPort}");
    ConsoleWig.WriteLine($"  ClientId : {settings.ClientId}");
    ConsoleWig.WriteLine($"  PubTopic : {settings.PublishTopic}");
    ConsoleWig.WriteLine($"  SubTopic : {settings.SubscribeTopic}");

    // Create client
    var factory = new MqttFactory();
    using var client = factory.CreateMqttClient();

    // Connect to broker
    ConsoleWig.WriteLine("Connecting to a broker");
    var connResult = await client.ConnectAsync(clientOptinos);

    // Start subscribe task
    var subResult = await client.SubscribeAsync(settings.SubscribeTopic);
    client.ApplicationMessageReceivedAsync += async recv =>
    {
        // This is not an exact code, so no special consideration is given to exclusions, etc.
        var pending = !Console.IsInputRedirected && 1 < Console.CursorLeft;
        if (recv.ReasonCode == MqttApplicationMessageReceivedReasonCode.Success)
        {
            var msg = recv.ApplicationMessage;
            ConsoleWig.WriteLineColored(ConsoleColor.DarkGray, $"\n{msg.Topic}: {msg.ConvertPayloadToString()}");
        }
        else
        {
            ConsoleWig.WriteLineColored(ConsoleColor.DarkGray, $"\n[WARN] {recv.ReasonCode}: {recv.ResponseReasonString}");
        }
        if (pending) ConsoleWig.Write("...");
        ConsoleWig.Write(">");
        await Task.CompletedTask;
    };

    // Publish messages. 
    ConsoleWig.WriteLine().WriteLine("Publish the input message.");
    while (true)
    {
        var message = ConsoleWig.Write(">").ReadLine();
        if (message.IsEmpty()) continue;
        await client.PublishStringAsync(settings.PublishTopic, message, cancellationToken: signal.Token);
    }

});
