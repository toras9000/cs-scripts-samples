// This script is meant to run with dotnet-script.
// You can install .NET SDK 7.0 and install dotnet-script with the following command.
// $ dotnet tool install -g dotnet-script

#r "nuget: KallitheaApiClient, 0.7.0.11"
#r "nuget: Lestaly, 0.42.0"
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
    KallitheaApiEntry = new Uri("http://localhost:8888/_admin/api"),

    // API key scramble save file.
    ScrambleFile = ThisSource.RelativeFile("kallithea-api.sav"),

    // Scramble save context (key)
    ScrambleContext = ThisSource.RelativeDirectory(".").FullName,
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
    Console.WriteLine($"Server API entry : {settings.KallitheaApiEntry}");

    // Attempt to read the stored API key information.
    var scrambler = new RoughScrambler(context: settings.ScrambleContext);
    var apiAccess = await scrambler.DescrambleObjectFromFileAsync<ApiAccessInfo>(settings.ScrambleFile, signal.Token);
    if (apiAccess?.Entry == settings.KallitheaApiEntry.AbsoluteUri)
    {
        // There was a stored key to the access point.
        Console.WriteLine($"API key decoded.");
    }
    else
    {
        // If it is not there, have them input it.
        apiAccess = new ApiAccessInfo(settings.KallitheaApiEntry.AbsoluteUri, ConsoleWig.Write("Input API key\n>").ReadLine());
    }

    // Obtaining repo information via API.
    Console.WriteLine($"Get repos info.");
    using var client = new SimpleKallitheaClient(settings.KallitheaApiEntry, apiAccess.Key);
    var repos = await client.GetReposAsync(signal.Token);

    // If API access is successful, scramble and save the API key.
    try { await scrambler.ScrambleObjectToFileAsync(settings.ScrambleFile, apiAccess, cancelToken: signal.Token); } catch { }

    // Saving options. Exclude some properties.
    var options = new SaveToCsvOptions();
    options.MemberFilter = m => m.Name switch { nameof(RepoInfo.last_changeset) => false, nameof(RepoInfo.landing_rev) => false, _ => true };

    // Save to csv.
    Console.WriteLine($"Save to csv.");
    var reposFile = ThisSource.RelativeFile($"repos_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    await repos.SaveToCsvAsync(reposFile, options);
});
