#load ".common.csx"
#nullable enable
using System.Text.RegularExpressions;
using System.Threading;
using KallitheaApiClient;
using KallitheaApiClient.Utils;
using Lestaly;

// Create a repository group along with adding users to create a simple dedicated area.
// Users to be registered are defined in a csv file.

var settings = new
{
    // API entry points for Kallithea.
    ApiEntry = new Uri("http://localhost:9999/_admin/api"),
};

// API Access Information
record ApiAccessInfo(string Entry, string Key);

// main processing
await Paved.RunAsync(configuration: o => o.AnyPause(), action: async () =>
{
    // Set output to UTF8 encoding.
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // Handle cancel key press
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    // Show access address
    Console.WriteLine($"Server API entry : {settings.ApiEntry}");

    // Attempt to recover saved API key information.
    var info = await ApiKeyStore.RestoreAsync(settings.ApiEntry, signal.Token);

    // Obtaining repo information via API.
    Console.WriteLine($"Get repos info.");
    using var client = new SimpleKallitheaClient(settings.ApiEntry, info.Key.Token);
    var repos = await client.GetReposAsync(signal.Token);

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync(signal.Token);

    // Saving options. Exclude some properties.
    var options = new SaveToCsvOptions();
    options.MemberFilter = m => m.Name switch { nameof(RepoInfo.last_changeset) => false, nameof(RepoInfo.landing_rev) => false, _ => true };

    // Save to csv.
    Console.WriteLine($"Save to csv.");
    var reposFile = ThisSource.RelativeFile($"repos_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    await repos.SaveToCsvAsync(reposFile, options);
});
