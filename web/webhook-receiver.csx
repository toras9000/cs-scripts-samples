#r "sdk:Microsoft.NET.Sdk.Web"
#r "nuget: Lestaly, 0.45.0"
#nullable enable
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Lestaly;

var settings = new
{
    // Accept port for HTTP service.
    PortNumber = 9978,

    // Server name
    ServerName = Environment.MachineName,

    // Accept endpoint path
    EndpointName = "webhook-accept",

    // Maximum output length of received JSON. If the value is less than or equal to zero, output the whole.
    MaxJsonOutputLength = -1,

    // Whether or not to dump the received JSON.
    DumpJsonEnabled = true,

    // Destination directory for JSON dump output.
    DumpJsonOutput = ThisSource.RelativeDirectory("webhook-receiver-dump"),
};

await Paved.RunAsync(async () =>
{
    // Set output encoding to UTF8.
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // Display URL to set up in BookStack.
    // This will be by the hostname added to the included docker container.
    Console.WriteLine($"Endpoint address:");
    Console.WriteLine($"    http://{settings.ServerName}:{settings.PortNumber}/{settings.EndpointName}");
    if (settings.DumpJsonEnabled)
    {
        Console.WriteLine($"Dump directory:");
        Console.WriteLine($"    {settings.DumpJsonOutput.FullName}");
    }
    Console.WriteLine();

    // Formatting options for outputting JSON.
    var jsonOpt = new JsonSerializerOptions();
    jsonOpt.WriteIndented = true;
    jsonOpt.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    // Web Server Configuration Builder
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();

    // Build a server instance
    var server = builder.Build();
    server.MapPost($"/{settings.EndpointName}", async (HttpRequest request) =>
    {
        // Endpoint for receiving Webhooks
        try
        {
            var body = await request.ReadFromJsonAsync<JsonElement>();
            var json = JsonSerializer.Serialize(body, jsonOpt);
            var showJson = (0 < settings.MaxJsonOutputLength) ? json.EllipsisByLength(settings.MaxJsonOutputLength, "...") : json;
            ConsoleWig.WriteLineColored(ConsoleColor.Green, $"{DateTime.Now}: Endpoint={request.Path}, JSON received.").WriteLine(showJson);
            if (settings.DumpJsonEnabled)
            {
                try
                {
                    var dumpFile = settings.DumpJsonOutput.RelativeFile($"{DateTime.Now:yyyyMMdd-HHmmss-fff}.json").WithDirectoryCreate();
                    await dumpFile.WriteAllTextAsync(body.GetRawText());
                }
                catch
                {
                    ConsoleWig.WriteLineColored(ConsoleColor.Yellow, $"{DateTime.Now}: Failed to dump.");
                }
            }
        }
        catch
        {
            ConsoleWig.WriteLineColored(ConsoleColor.Yellow, $"{DateTime.Now}: Endpoint={request.Path}, Not JSON.");
        }

        return Results.Ok();
    });
    server.MapFallback((HttpRequest request) =>
    {
        ConsoleWig.WriteLineColored(ConsoleColor.DarkGray, $"{DateTime.Now}: Ignore request, Method={request.Method}, Path={request.Path}");
        return Results.NotFound();
    });

    // Start HTTP Server
    Console.WriteLine($"Start HTTP service.");
    await server.RunAsync($"http://*:{settings.PortNumber}");
});
