#!/usr/bin/env dotnet-script
#r "nuget: NuGet.Protocol, 7.0.0"
#r "nuget: R3, 1.3.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.112.0"
#nullable enable
using System.Text.RegularExpressions;
using Kokuban;
using Lestaly;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using R3;

// This script requires a isolated assembly context.
// `dotnet script --isolated-load-context ./@update-packages.csx`

var settings = new
{
    // Search directory for script files
    TargetDir = ThisSource.RelativeDirectory("./"),

    // Version of the package to pin
    FixedVersions = new PackageIdentity[]
    {
        new("Dummy",   new("0.1.0-preview1")),
    },
};

return await Paved.ProceedAsync(async () =>
{
    // package sources
    var config = NuGet.Configuration.Settings.LoadDefaultSettings(default);
    var sources = NuGet.Configuration.PackageSourceProvider.LoadPackageSources(config).ToArray();
    var seachers = await sources.ToObservable()
        .SelectAwait(async (s, c) => await Repository.Factory.GetCoreV3(s).GetResourceAsync<PackageMetadataResource>(c))
        .ToArrayAsync();

    // find context
    var cache = new SourceCacheContext();
    var logger = NullLogger.Instance;

    // Dictionary of packages to be updated
    var versions = settings.FixedVersions.ToDictionary(p => p.Id, p => (PackageIdentity?)p);

    // Detection regular expression for package reference directives
    var detector = new Regex(@"^\s*#\s*r\s+""\s*nuget\s*:\s*(?<package>[a-zA-Z0-9_\-\.]+)(?:,| )\s*(?<version>.+)\s*""");

    // Search for scripts under the target directory
    foreach (var file in settings.TargetDir.EnumerateFiles("*.csx", SearchOption.AllDirectories))
    {
        WriteLine($"File: {file.RelativePathFrom(settings.TargetDir)}");

        // Read file contents
        var lines = await file.ReadAllLinesAsync();

        // Attempt to update package references
        var detected = false;
        var updated = false;
        for (var i = 0; i < lines.Length; i++)
        {
            // Detecting Package Reference Directives
            var line = lines[i];
            var match = detector.Match(line);
            if (!match.Success) continue;
            detected = true;

            // Determine if the package is eligible for renewal
            var srcName = match.Groups["package"].Value;
            var srcVer = match.Groups["version"].Value;
            if (!versions.TryGetValue(srcName, out var identity))
            {
                // Interpret the original version to determine if it is a pre-release.
                var usePrerelease = NuGetVersion.TryParse(srcVer, out var ver) && ver.IsPrerelease;

                // Retrieve the latest package version.
                var metadatas = await seachers.ToObservable()
                    .SelectAwait(async (r, c) => await r.GetMetadataAsync(srcName, includePrerelease: usePrerelease, includeUnlisted: false, cache, logger, c))
                    .SelectMany(vers => vers.ToObservable())
                    .ToArrayAsync();
                var latest = metadatas.MaxBy(m => m.Identity.Version, VersionComparer.Default);
                WriteLine((latest == null ? Chalk.Yellow : Chalk.Gray)[$"  Info: {srcName} - Retrieve the latest version information: {(latest == null ? "Failed" : "Success")}"]);

                // Set as the target version for update
                identity = latest?.Identity;

                // Added to package dictionary
                versions.Add(srcName, identity);
            }

            // Evaluate acquisition result
            if (identity == null)
            {
                WriteLine(Chalk.Yellow[$"  Skip: {srcName} - Unable to retrieve the package version"]);
                continue;
            }

            // Determine if the package version needs to be updated.
            var latestVer = identity.Version.ToFullString();
            if (srcVer == latestVer)
            {
                WriteLine(Chalk.Gray[$"  Skip: {srcName} - Already in version"]);
                continue;
            }

            // Create a replacement line for the reference directive
            var newLine = @$"#r ""nuget: {srcName}, {latestVer}""";
            lines[i] = newLine;

            // set a flag that there is an update
            updated = true;
            WriteLine(Chalk.Green[$"  Update: {srcName} {srcVer} -> {latestVer}"]);
        }

        // Write back to file if updates are needed
        if (updated)
        {
            await file.WriteAllLinesAsync(lines);
        }
        else if (!detected)
        {
            WriteLine(Chalk.Gray[$"  Directive not found"]);
        }
    }

});
