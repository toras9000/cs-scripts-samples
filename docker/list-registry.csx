#r "nuget: Docker.Registry.DotNet, 2.0.0"
#r "nuget: Lestaly, 0.102.0"
#nullable enable
using System.Threading;
using Docker.Registry.DotNet;
using Docker.Registry.DotNet.Domain.ImageReferences;
using Docker.Registry.DotNet.Domain.Manifests;
using Lestaly;

var settings = new
{
    // Docker registry URL
    Url = "https://registry.toras.home",
};

return await Paved.ProceedAsync(async () =>
{
    // Handle cancel key press
    using var signal = new SignalCancellationPeriod();

    // Generate anonymous access client for target registry
    WriteLine($"Registry: {settings.Url}");
    var config = new RegistryClientConfiguration(settings.Url);
    using var client = config.CreateClient();

    // Retrieve Image Catalog
    var catalog = await client.Catalog.GetCatalog(parameters: null, signal.Token);

    // Process each image in the catalog
    WriteLine("Images:");
    foreach (var repo in catalog.Repositories)
    {
        // Get image tag list
        var tagsResult = await client.Tags.ListTags(repo, parameters: null, signal.Token);

        // Process each tag
        foreach (var tag in tagsResult.Tags)
        {
            // Image/Tag name
            WriteLine($"  {repo}:{tag.Value}");

            // Get tag manifest
            var tagManifest = await client.Manifest.GetManifest(repo, new(tag), signal.Token);

            // Make additional information for the manifest
            static string imageInfo(ImageManifest manifest) => manifest switch
            {
                ImageManifest2_2 v2 => $"Size={v2.Layers!.Sum(l => l.Size).ToHumanize()}iB, Digest={v2.Config!.Digest!.SkipToken(':')[..12]}",
                _ => "",
            };

            // Processing by Manifest Type
            switch (tagManifest.Manifest)
            {
                case ManifestList list:
                    foreach (var item in list.Manifests ?? [])
                    {
                        if (item == null || item.Digest == null || item.Platform == null) continue;
                        var itemManifest = await client.Manifest.GetManifest(repo, new(new ImageDigest(item.Digest)), signal.Token);
                        var archName = new[] { item.Platform.Os, item.Platform.Architecture, item.Platform.Variant, }.DropWhite().JoinString("/");
                        WriteLine($"    Arch={archName}{", ".TieIn(imageInfo(itemManifest.Manifest))}");
                    }
                    break;

                default:
                    WriteLine($"    Arch=(unknown){", ".TieIn(imageInfo(tagManifest.Manifest))}");
                    break;
            }
        }

    }
});
