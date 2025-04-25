#r "nuget: MQTTnet, 5.0.1.1416"
#r "nuget: Lestaly, 0.75.0"
#nullable enable
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Lestaly;
using MQTTnet;

var settings = new
{
    BrokerHost = "test.mosquitto.org",

    BrokerPort = 8884,

    CaCert = ThisSource.RelativeFile("./certs/ca.crt"),

    // https://test.mosquitto.org/ssl/
    ClientCert = ThisSource.RelativeFile("./certs/client.crt"),

    ClientKey = ThisSource.RelativeFile("./certs/client.key"),

    ClientId = $"test-cid-{Guid.NewGuid()}",

    PublishTopic = "mytest/test/aaa",
};

return await Paved.RunAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Preparing to connect");

    // Load certs
    // Temporary cert and Export/Reload are to avoid the following errors by Windows : 'Authentication failed because the platform does not support ephemeral keys.'
    using var caCert = X509Certificate2.CreateFromPem(await settings.CaCert.ReadAllTextAsync());
    using var clientCertTemp = X509Certificate2.CreateFromPemFile(settings.ClientCert.FullName, settings.ClientKey.FullName);
    using var clientCert = X509CertificateLoader.LoadPkcs12(clientCertTemp.Export(X509ContentType.Pkcs12), "");

    // TLS options
    var tlsOptions = new MqttClientTlsOptionsBuilder()
        .UseTls()
        .WithTargetHost(settings.BrokerHost)
        .WithTrustChain(new() { caCert, })
        .WithClientCertificates(new() { clientCert, })
        .WithApplicationProtocols(new() { SslApplicationProtocol.Http2, })
        .Build();

    // Client optinos
    var clientOptinos = new MqttClientOptionsBuilder()
        .WithTcpServer(settings.BrokerHost, settings.BrokerPort)
        .WithTlsOptions(tlsOptions)
        .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
        .WithCleanSession(true)
        .WithWillRetain(false)
        .WithClientId(settings.ClientId)
        .Build();

    // Context information
    WriteLine("Context information");
    WriteLine($"  Broker       : {settings.BrokerHost}:{settings.BrokerPort}");
    WriteLine($"  ClientId     : {settings.ClientId}");
    WriteLine($"  PublishTopic : {settings.PublishTopic}");

    // Create client
    var factory = new MqttClientFactory();
    using var client = factory.CreateMqttClient();

    // Connect to broker
    WriteLine("Connecting to a broker");
    var connResult = await client.ConnectAsync(clientOptinos);

    // Publish messages. 
    WriteLine(); WriteLine("Publish the input message.");
    while (true)
    {
        Write(">");
        var message = ReadLine();
        if (message.IsEmpty()) continue;
        await client.PublishStringAsync(settings.PublishTopic, message, cancellationToken: signal.Token);
    }

});
