#r "nuget: Lestaly, 0.56.0"
#nullable enable
using System.Text.RegularExpressions;
using Lestaly;

// Rewrite the version of the script to the specified version.
// The vscode/C# extension recognizes only one package version for all scripts in the workspace.
// If there are discrepancies, the IntelliSense will not work properly, so the versions should be aligned.

var settings = new
{
    // Search directory for script files
    TargetDir = ThisSource.RelativeDirectory("../"),

    // Packages and versions to be unified and updated
    Packages = new PackageVersion[]
    {
        new("Lestaly",                     "0.56.0"),
        new("Docker.Registry.DotNet",      "1.2.1"),
        new("SkiaSharp",                   "2.88.7"),
        new("System.Data.SQLite.Core",     "1.0.118"),
        new("MQTTnet",                     "4.3.3.952"),
    },
};

// Package version information data type
record PackageVersion(string Name, string Version);

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Detection regular expression for package reference directives
    var detector = new Regex(@"^\s*#\s*r\s+""\s*nuget\s*:\s*([a-zA-Z0-9_\-\.]+)(?:,| )\s*(\d+)([0-9\-\.]+)?\s*""");

    // Dictionary of packages to be updated
    var versions = settings.Packages.ToDictionary(p => p.Name);

    // Search for scripts under the target directory
    foreach (var file in settings.TargetDir.EnumerateFiles("*.csx", SearchOption.AllDirectories))
    {
        Console.WriteLine($"{file.RelativePathFrom(settings.TargetDir, ignoreCase: true)}");

        // Read file contents
        var lines = await file.ReadAllLinesAsync();

        // Attempt to update package references
        var updated = false;
        for (var i = 0; i < lines.Length; i++)
        {
            // Detecting Package Reference Directives
            var line = lines[i];
            var match = detector.Match(line);
            if (!match.Success) continue;

            // Determine if the package is eligible for renewal
            var pkgName = match.Groups[1].Value;
            if (!versions.TryGetValue(pkgName, out var package))
            {
                Console.WriteLine($"  Skip: {pkgName} - Not update target");
                continue;
            }

            // Determine if the package version needs to be updated.
            var pkgVer = match.Groups[2].Value + match.Groups[3].Value;
            if (pkgVer == package.Version)
            {
                Console.WriteLine($"  Skip: {pkgName} - Already in version");
                continue;
            }

            // Create a replacement line for the reference directive
            var additional = line[(match.Index + match.Length)..];
            var newLine = @$"#r ""nuget: {pkgName}, {package.Version}""{additional}";
            lines[i] = newLine;

            // set a flag that there is an update
            updated = true;
            Console.WriteLine($"  Update: {pkgName} {pkgVer} -> {package.Version}");
        }

        // Write back to file if updates are needed
        if (updated)
        {
            await file.WriteAllLinesAsync(lines);
        }
    }

});
