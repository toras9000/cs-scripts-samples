#r "nuget: Lestaly, 0.56.0"
#nullable enable
using System.Diagnostics.CodeAnalysis;
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
        new("Lestaly",                               "0.56.0"),
        new("Docker.Registry.DotNet",                "1.2.1"),
        new("SkiaSharp",                             "2.88.7"),
        new("MQTTnet",                               "4.3.3.952"),
        new("System.Data.SQLite.Core",               "1.0.118"),
        new("Npgsql.EntityFrameworkCore.PostgreSQL", "8.0.0"),
        new("Microsoft.EntityFrameworkCore.Sqlite",  "8.0.1"),
        new("ClosedXML",                             "0.104.0-preview2"),
        new("Kokuban",                               "0.2.0"),
    },
};

// Package version information data type
record PackageVersion(string Name, string Version)
{
    public SemanticVersion SemanticVersion { get; } = new SemanticVersion(Version);
}

// Data type for version value management
record SemanticVersion
{
    public SemanticVersion(string version)
    {
        var match = VersionPattern.Match(version);
        if (!match.Success) throw new ArgumentException("Illegal");
        this.Major = int.Parse(match.Groups["major"].Value);
        this.Minor = int.Parse(match.Groups["subver"].Captures[0].Value);
        this.Patch = int.TryParse(match.Groups["subver"].Captures.ElementAtOrDefault(1)?.Value, out var patch) ? patch : default;
        this.Filum = int.TryParse(match.Groups["subver"].Captures.ElementAtOrDefault(2)?.Value, out var filum) ? filum : default;
        this.PreRelease = match.Groups["pre"].Value;
        this.Build = match.Groups["build"].Value;
    }

    public int Major { get; }
    public int Minor { get; }
    public int? Patch { get; }
    public int? Filum { get; }
    public string PreRelease { get; }
    public string Build { get; }

    public static bool TryParse(string text, [NotNullWhen(true)] out SemanticVersion? version)
    {
        try { version = new SemanticVersion(text); return true; }
        catch { version = default; return false; }
    }

    private static readonly Regex VersionPattern = new(@"^(?<major>\d+)(?:\.(?<subver>\d+)){1,3}(?:\-(?<pre>.+))?(?:\+(?<build>.+))?$");
}

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Detection regular expression for package reference directives
    var detector = new Regex(@"^\s*#\s*r\s+""\s*nuget\s*:\s*([a-zA-Z0-9_\-\.]+)(?:,| )\s*(.+)\s*""");

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

            // Parse the version number.
            if (!SemanticVersion.TryParse(match.Groups[2].Value, out var pkgVer))
            {
                Console.WriteLine($"  Skip: Unable to recognize version number");
                continue;
            }

            // Determine if the package version needs to be updated.
            if (pkgVer == package.SemanticVersion)
            {
                Console.WriteLine($"  Skip: {pkgName} - Already in version");
                continue;
            }

            // Create a replacement line for the reference directive
            var newLine = @$"#r ""nuget: {pkgName}, {package.Version}""";
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
