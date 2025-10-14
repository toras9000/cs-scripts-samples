#r "nuget: MQTTnet, 5.0.1.1416"
#r "nuget: Lestaly.General, 0.106.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Lestaly;
using Kokuban;
using MQTTnet;

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
    using var signal = new SignalCancellationPeriod();

    // Client optinos
    var clientOptinos = new MqttClientOptionsBuilder()
        .WithTcpServer(settings.BrokerHost, settings.BrokerPort)
        .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
        .WithCleanSession(true)
        .WithWillRetain(false)
        .WithClientId(settings.ClientId)
        .Build();

    // Context information
    WriteLine("Context information");
    WriteLine($"  Broker   : {settings.BrokerHost}:{settings.BrokerPort}");
    WriteLine($"  ClientId : {settings.ClientId}");
    WriteLine($"  PubTopic : {settings.PublishTopic}");
    WriteLine($"  SubTopic : {settings.SubscribeTopic}");

    // Create client
    var factory = new MqttClientFactory();
    using var client = factory.CreateMqttClient();

    // Connect to broker
    WriteLine("Connecting to a broker");
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
            WriteLine(Chalk.Gray[$"\n{msg.Topic}: {msg.ConvertPayloadToString()}"]);
        }
        else
        {
            WriteLine(Chalk.Gray[$"\n[WARN] {recv.ReasonCode}: {recv.ResponseReasonString}"]);
        }
        if (pending) Write("...");
        Write(">");
        await Task.CompletedTask;
    };

    // Publish messages. 
    WriteLine();
    WriteLine("Publish the input message.");
    while (true)
    {
        Write(">");
        var message = ReadLine();
        if (message.IsEmpty()) continue;
        await client.PublishStringAsync(settings.PublishTopic, message, cancellationToken: signal.Token);
    }

});
