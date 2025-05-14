#r "sdk:Microsoft.NET.Sdk.Web"
#r "nuget: Lestaly, 0.80.0"
#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Lestaly;
using Microsoft.Extensions.DependencyInjection;

var settings = new
{
    // Accept port for HTTP service.
    PortNumber = 9979,

    // Server name
    ServerName = Environment.MachineName,

    // Server name
    LocalContents = ThisSource.RelativeDirectory(".."),
};

await Paved.RunAsync(async () =>
{
    // Set output encoding to UTF8.
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // Display URL to set up in BookStack.
    // This will be by the hostname added to the included docker container.
    var serverAddr = $"http://{settings.ServerName}:{settings.PortNumber}";
    Console.WriteLine($"Server address:");
    Console.WriteLine($"    {serverAddr}");
    Console.WriteLine($"Local Path:");
    Console.WriteLine($"    {settings.LocalContents.FullName}");
    Console.WriteLine();

    // Web Server Configuration Builder
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.Logging.AddConsoleFormatter<TinyConsoleFormatter, ConsoleFormatterOptions>();
    builder.Logging.AddConsole(c => c.FormatterName = nameof(TinyConsoleFormatter));
    builder.Services.AddDirectoryBrowser();

    // Service for static file references
    var browseOptions = new FileServerOptions();
    browseOptions.EnableDirectoryBrowsing = true;
    browseOptions.StaticFileOptions.ContentTypeProvider = new BinaryContentTypeProvider();
    browseOptions.FileProvider = new PhysicalFileProvider(settings.LocalContents.FullName);

    // Build a server instance
    var server = builder.Build();
    server.UseFileServer(browseOptions);

    // Start HTTP Server
    Console.WriteLine($"Start HTTP service.");
    var instance = server.RunAsync($"http://*:{settings.PortNumber}/");

    // Open Server URL in browser
    await CmdShell.ExecAsync(serverAddr);

    // Wait for server instance
    await instance;
});

// A provider that treats all files as binary.
class BinaryContentTypeProvider : IContentTypeProvider
{
    public bool TryGetContentType(string subpath, [MaybeNullWhen(false)] out string contentType)
    {
        contentType = "application/octet-stream";
        return true;
    }
}

// Tiny formatter
class TinyConsoleFormatter : ConsoleFormatter
{
    public TinyConsoleFormatter() : base(nameof(TinyConsoleFormatter)) { }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        // Skip response log
        if (logEntry.EventId.Id == 2) return;

        // Format message
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message == null) return;

        // output message
        textWriter.WriteLine($"{DateTime.Now:G} {message}");
    }
}
