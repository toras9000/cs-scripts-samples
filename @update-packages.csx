#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly, 0.84.0"
#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Kokuban;
using Lestaly;

// Rewrite the version of the script to the specified version.
// The vscode/C# extension recognizes only one package version for all scripts in the workspace.
// If there are discrepancies, the IntelliSense will not work properly, so the versions should be aligned.

var settings = new
{
    // Search directory for script files
    TargetDir = ThisSource.RelativeDirectory("./"),

    // Packages and versions to be unified and updated
    Packages = new PackageVersion[]
    {
        new("Lestaly",                               "0.84.0"),
        new("Docker.Registry.DotNet",                "2.0.0"),
        new("SkiaSharp",                             "3.119.0"),
        new("MQTTnet",                               "5.0.1.1416"),
        new("AngleSharp",                            "1.3.0"),
        new("Microsoft.Playwright",                  "1.52.0"),
        new("StreamJsonRpc",                         "2.22.11"),
        new("System.Data.SQLite.Core",               "1.0.119"),
        new("Npgsql.EntityFrameworkCore.PostgreSQL", "9.0.4"),
        new("Microsoft.EntityFrameworkCore.Sqlite",  "9.0.5"),
        new("ClosedXML",                             "0.105.0"),
        new("Kokuban",                               "0.2.0"),
        new("WebSerializer",                         "1.3.0"),
        new("System.DirectoryServices",              "9.0.5"),
        new("System.DirectoryServices.Protocols",    "9.0.5"),
    },
};

return await Paved.ProceedAsync(async () =>
{
    // Detection regular expression for package reference directives
    var detector = new Regex(@"^\s*#\s*r\s+""\s*nuget\s*:\s*(?<package>[a-zA-Z0-9_\-\.]+)(?:,| )\s*(?<version>.+)\s*""");

    // Dictionary of packages to be updated
    var versions = settings.Packages.ToDictionary(p => p.Name);

    // Search for scripts under the target directory
    foreach (var file in settings.TargetDir.EnumerateFiles("*.csx", SearchOption.AllDirectories))
    {
        Console.WriteLine($"File: {file.RelativePathFrom(settings.TargetDir, ignoreCase: true)}");

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
            var pkgName = match.Groups["package"].Value;
            if (!versions.TryGetValue(pkgName, out var package))
            {
                Console.WriteLine(Chalk.BrightYellow[$"  Skip: {pkgName} - Not update target"]);
                continue;
            }

            // Parse the version number.
            if (!SemanticVersion.TryParse(match.Groups["version"].Value, out var pkgVer))
            {
                Console.WriteLine(Chalk.BrightYellow[$"  Skip: Unable to recognize version number"]);
                continue;
            }

            // Determine if the package version needs to be updated.
            if (pkgVer == package.SemanticVersion)
            {
                Console.WriteLine(Chalk.BrightYellow[$"  Skip: {pkgName} - Already in version"]);
                continue;
            }

            // Create a replacement line for the reference directive
            var newLine = @$"#r ""nuget: {pkgName}, {package.Version}""";
            lines[i] = newLine;

            // set a flag that there is an update
            updated = true;
            Console.WriteLine(Chalk.Green[$"  Update: {pkgName} {pkgVer.Original} -> {package.Version}"]);
        }

        // Write back to file if updates are needed
        if (updated)
        {
            await file.WriteAllLinesAsync(lines);
        }
        else if (!detected)
        {
            Console.WriteLine(Chalk.BrightYellow[$"  Directive not found"]);
        }
    }

});

// Package version information data type
record PackageVersion(string Name, string Version)
{
    public SemanticVersion SemanticVersion { get; } = SemanticVersion.Parse(Version);
}
