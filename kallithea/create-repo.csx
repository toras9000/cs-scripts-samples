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
    ServiceUrl = new Uri("http://localhost:9999"),

    // The Kallithea repository group where the repository was created. 
    KallitheaPlace = "works",

    // 
    LocalPlace = ThisSource.RelativeDirectory("repos"),
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
    Console.WriteLine($"Service URL : {settings.ServiceUrl}");

    // Attempt to recover saved API key information.
    var apiEntry = new Uri(settings.ServiceUrl, "_admin/api");
    var info = await ApiKeyStore.RestoreAsync(apiEntry, signal.Token);

    // Create client
    using var client = new SimpleKallitheaClient(apiEntry, info.Key.Token);

    // 
    var me = await client.GetUserAsync();

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync(signal.Token);

    // 
    var repoGrp = await client.GetRepoGroupInfoAsync(new(settings.KallitheaPlace), signal.Token);

    // 
    var grpPath = repoGrp?.group_name;
    ConsoleWig.WriteLine($"Create a repository in '{grpPath.WhenWhite("top level")}'");
    var name = ConsoleWig.Write("Enter the name of the repository to be created.\n>").ReadLine().CancelIfWhite();

    var repoPath = grpPath.Mux(name, "/");
    await client.CreateRepoAsync(new(repoPath, repo_type: RepoType.git), signal.Token);

    var repoUri = new Uri(settings.ServiceUrl, repoPath);
    

});
