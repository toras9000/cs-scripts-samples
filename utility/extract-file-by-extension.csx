#r "nuget: Lestaly.General, 0.106.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Text.RegularExpressions;
using Lestaly;
using Kokuban;

var settings = new
{
    // Location of the base directory from which to create the destination directory
    ExtractBaseDir = ThisSource.RelativeDirectory("."),
};

try
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Enter source directory
    WriteLine("Enter source directory"); Write(">");
    var searchInput = ReadLine().CancelIfWhite();
    var searchDir = CurrentDir.RelativeDirectory(searchInput).ThrowIfNotExists(d => new PavedMessageException($"Directory '{d.FullName}' does not exist."));
    WriteLine();

    // Input Extension to be Extracted
    WriteLine("Enter the target extension"); Write(">");
    var extInput = ReadLine().CancelIfWhite();
    var extDotted = extInput.EnsureStarts(".");
    WriteLine();

    // Destination directory
    var extractDir = settings.ExtractBaseDir.RelativeDirectory($"extract-ext-{searchDir.Name}-{DateTime.Now:yyyyMMdd_HHmmss}").WithCreate();

    // Search options
    var searchOptions = new EnumerationOptions();
    searchOptions.IgnoreInaccessible = false;
    searchOptions.MatchCasing = MatchCasing.CaseInsensitive;
    searchOptions.MatchType = MatchType.Simple;
    searchOptions.RecurseSubdirectories = true;
    searchOptions.ReturnSpecialDirectories = false;

    // Search 
    WriteLine("Extracting the target file");
    var count = 0;
    foreach (var srcFile in searchDir.EnumerateFiles($"*{extDotted}", searchOptions))
    {
        // Obtaining a path relative to the search target
        var relativePath = srcFile.RelativePathFrom(searchDir, ignoreCase: true);

        // Copy and extract files
        var destFile = extractDir.RelativeFile(relativePath).WithDirectoryCreate();
        srcFile.CopyTo(destFile.FullName);

        // 
        count++;
        if (count % 100 == 0) WriteLine($"..{count} files copied");
    }

    // Final result display
    if (count % 100 != 0) WriteLine($"..{count} files copied");

    // Completion indication
    WriteLine();
    WriteLine("Extraction complete");
}
catch (Exception ex)
{
    WriteLine($"Error: {ex.Message}");
}
if (!IsInputRedirected)
{
    WriteLine();
    WriteLine("(press any key to exit)");
    ReadKey(intercept: true);
}
