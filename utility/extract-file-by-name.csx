#r "nuget: Lestaly, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Kurukuru, 1.4.2"
#nullable enable
using System.Text.RegularExpressions;
using Lestaly;
using Kokuban;
using Kurukuru;

var settings = new
{
    // Location of the base directory from which to create the destination directory
    ExtractBaseDir = ThisSource.RelativeDirectory("."),

    // Whether or not a directory (alone) should be extracted.
    // Directories matching the pattern are extracted as empty directories.
    ExtractDirectory = true,

    // Whether the file is to be extracted or not.
    ExtractFile = true,
};

return await Paved.ProceedAsync(async () =>
{
    // Prepare console
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Enter source directory
    WriteLine("Enter source directory"); Write(">");
    var searchInput = ReadLine().CancelIfWhite();
    var searchDir = CurrentDir.RelativeDirectory(searchInput).ThrowIfNotExists(d => new PavedMessageException($"Directory '{d.FullName}' does not exist."));
    WriteLine();

    // Input regular expression to filter the extraction target
    WriteLine("Input regular expression to be extracted"); Write(">");
    var patternInput = ReadLine().CancelIfWhite();
    var patternRegex = new Regex(patternInput);
    WriteLine();

    // Destination directory
    var extractDir = settings.ExtractBaseDir.RelativeDirectory($"extract-pat-{searchDir.Name}-{DateTime.Now:yyyyMMdd_HHmmss}").WithCreate();

    // Search options
    var searchOptions = new SelectFilesOptions(
        Recurse: true,
        Handling: new(File: settings.ExtractFile, Directory: settings.ExtractDirectory),
        Sort: false,
        Buffered: false
    );

    // Search
    WriteLine("Extracting the target");
    await Spinner.StartAsync($"Extracting ...", async spinner =>
    {
        // dummy
        await Task.CompletedTask;

        // Searches under directories and performs extraction processing.
        var count = 0;
        searchDir.DoFiles(options: searchOptions, processor: walker =>
        {
            // Determine if the found target is a target for extraction. If not, no processing.
            if (!patternRegex.IsMatch(walker.Item.Name)) return;

            // Obtaining a path relative to the search target
            var relativePath = walker.Item.RelativePathFrom(searchDir, ignoreCase: true);

            // Branch processing depending on whether it is a file or a directory.
            if (walker.File == null)
            {
                // Create a directory with the same path
                extractDir.RelativeDirectory(relativePath).Create();
            }
            else
            {
                // Extract by copying the file.
                var destFile = extractDir.RelativeFile(relativePath).WithDirectoryCreate();
                walker.File.CopyTo(destFile.FullName);
            }

            // Show progress
            count++;
            if (count % 100 == 0) spinner.Text = $"Extracting ... {count} items processed";
        });

        // Final result display
        if (count % 100 != 0) spinner.Text = $"Extraction complete ... {count} items processed";
    });

});
