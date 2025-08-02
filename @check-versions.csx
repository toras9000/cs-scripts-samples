#r "nuget: NuGet.Protocol, 6.14.0"
#r "nuget: R3, 1.3.0"
#r "nuget: Lestaly, 0.102.0"
#nullable enable
using Lestaly;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using R3;

// This script requires a isolated assembly context.
// `dotnet script --isolated-load-context ./@check-versions.csx`

return await Paved.ProceedAsync(async () =>
{
    // collect target packages info
    var targetPackages = ThisSource.RelativeFile("@update-packages.csx").ReadAllLines()
        .SkipWhile(line => !line.IsMatch(@"Packages\s*=\s*new\s+PackageVersion\s*\[\s*\]"))
        .Skip(1)
        .TakeWhile(line => !line.TrimStart().StartsWith("}"))
        .Select(line => line.Match(@"^\s*new\s*\(""(?<name>.+?)""\s*,\s*""(?<version>.+?)""\s*\)"))
        .Where(match => match.Success)
        .Select(match => (name: match.Groups["name"].Value, version: match.Groups["version"].Value))
        .ToArray();

    // package sources
    var settings = NuGet.Configuration.Settings.LoadDefaultSettings(default);
    var sources = NuGet.Configuration.PackageSourceProvider.LoadPackageSources(settings).ToArray();
    var seachers = await sources.ToObservable()
        .SelectAwait(async (s, c) => await Repository.Factory.GetCoreV3(s).GetResourceAsync<PackageMetadataResource>(c))
        .ToArrayAsync();

    // find context
    var cache = new SourceCacheContext();
    var logger = NullLogger.Instance;

    // check versions
    var latestVersions = new List<(string name, string version)>();
    foreach (var target in targetPackages)
    {
        // parse current version
        if (!NuGetVersion.TryParse(target.version, out var targetVer))
        {
            latestVersions.Add((target.name, target.version));
            continue;
        }

        // get latest version
        var metadatas = await seachers.ToObservable()
            .SelectAwait(async (r, c) => await r.GetMetadataAsync(target.name, includePrerelease: targetVer.IsPrerelease, includeUnlisted: false, cache, logger, c))
            .SelectMany(vers => vers.ToObservable())
            .ToArrayAsync();
        var latest = metadatas.MaxBy(m => m.Identity.Version, VersionComparer.Default);
        latestVersions.Add((target.name, (latest?.Identity.Version ?? targetVer).ToFullString()));
    }

    // print list
    WriteLine("Latest versions:");
    var nameWidth = latestVersions.Max(v => v.name.Length);
    var verWidth = latestVersions.Max(v => v.version.Length);
    foreach (var pkg in latestVersions)
    {
        var namePad = "".PadRight(nameWidth - pkg.name.Length);
        var verPad = "".PadRight(verWidth - pkg.version.Length);
        WriteLine($"        new(\"{pkg.name}\",{namePad}   \"{pkg.version}\"{verPad}  ),");
    }
});
