#load ".common.csx"
#nullable enable
using KallitheaApiClient;
using KallitheaApiClient.Utils;
using Lestaly;

var settings = new
{
    // API entry points for Kallithea.
    ApiEntry = new Uri("http://localhost:9999/_admin/api"),
};

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
    Console.WriteLine($"Get my permissions.");
    using var client = new SimpleKallitheaClient(settings.ApiEntry, info.Key.Token);

    // Get user information.
    var user = await client.GetUserAsync();
    Console.WriteLine($"User: {user.user.username}");

    // If API access is successful, scramble and save the API key.
    await info.SaveAsync(signal.Token);

    // Print a list of permissions to the repository.
    if (0 < user.permissions.repositories.Count)
    {
        Console.WriteLine($"  RepoPerms:");
        foreach (var repoPerm in user.permissions.repositories.OrderBy(e => e.name))
        {
            Console.WriteLine($"    {repoPerm.name}: {repoPerm.value.ToPermName()}");
        }
    }
    // Print a list of permissions to the repository group.
    if (0 < user.permissions.repositories_groups.Count)
    {
        Console.WriteLine($"  RepoGroupPerms:");
        foreach (var grpPerm in user.permissions.repositories_groups)
        {
            Console.WriteLine($"    {grpPerm.name}: {grpPerm.value.ToPermName()}");
        }
    }

});
