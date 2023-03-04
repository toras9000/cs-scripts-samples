#nullable enable
#r "nuget: Docker.Registry.DotNet, 1.2.1"
#r "nuget: Lestaly, 0.32.0"
using Lestaly;
using Docker.Registry.DotNet;
using Docker.Registry.DotNet.Authentication;
using System.Threading;
using Docker.Registry.DotNet.Models;

var settings = new
{
    // Docker registry URL
    Url = "registry.toras.home",
};

return await Paved.RunAsync(configuration: o => o.PauseOnExit = true, action: async () =>
{
    // Handle cancel key press
    using var canceller = new CancellationTokenSource();
    using var handler = ConsoleWig.CancelKeyHandlePeriod(canceller);

    // Generate anonymous access client for target registry
    Console.WriteLine($"Registry: {settings.Url}");
    var config = new RegistryClientConfiguration(settings.Url);
    var authenticator = new AnonymousOAuthAuthenticationProvider();
    using var client = config.CreateClient(authenticator);

    // Retrieve Image Catalog
    var catalog = await client.Catalog.GetCatalogAsync(parameters: null, canceller.Token);

    // Process each image in the catalog
    Console.WriteLine("Images:");
    foreach (var repo in catalog.Repositories)
    {
        // Get image tag list
        var tagsResult = await client.Tags.ListImageTagsAsync(repo, parameters: null, canceller.Token);

        // Process each tag
        foreach (var tag in tagsResult.Tags)
        {
            // Image/Tag name
            Console.WriteLine($"  {repo}:{tag}");

            // Get tag manifest
            var tagManifest = await client.Manifest.GetManifestAsync(repo, tag, canceller.Token);

            // Make additional information for the manifest
            static string imageInfo(ImageManifest manifest) => manifest switch
            {
                ImageManifest2_2 v2 => $"Size={v2.Layers.Sum(l => l.Size).ToHumanize()}iB, Digest={v2.Config.Digest.AfterAt(':')[..12]}",
                _ => "",
            };

            // Processing by Manifest Type
            switch (tagManifest.Manifest)
            {
                case ManifestList list:
                    foreach (var item in list.Manifests)
                    {
                        var itemManifest = await client.Manifest.GetManifestAsync(repo, item.Digest, canceller.Token);
                        var archName = new[] { item.Platform.Os, item.Platform.Architecture, item.Platform.Variant, }.DropWhite().JoinString("/");
                        Console.WriteLine($"    Arch={archName}{", ".TieIn(imageInfo(itemManifest.Manifest))}");
                    }
                    break;

                default:
                    Console.WriteLine($"    Arch=(unknown){", ".TieIn(imageInfo(tagManifest.Manifest))}");
                    break;
            }
        }

    }
});
