#r "nuget: Lestaly.General, 0.108.0"
#r "nuget: Lestaly.Excel, 0.100.0"
#nullable enable
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Lestaly;

// Command line argument mapping type
var settings = new
{
    // Exclusion patterns from search. (regex)
    ExcludePatterns = new Regex[] { new("^.git"), new("^.hg"), new("^.svn"), },

    // Flag to output full path.
    OutputFullName = false,

    // Flag to output as Excel file.
    FormatExcel = false,
};

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("Search Directory");
    Write(">");
    var searchDir = ReadLine().Unquote()?.AsDirectoryInfo();
    if (searchDir == null) return;

    // search options
    var options = new VisitFilesOptions(
        Recurse: true,
        SkipInaccessible: true,
        Handling: VisitFilesHandling.OnlyFile,
        FileFilter: file => !settings.ExcludePatterns.Any(re => re.IsMatch(file.Name)),
        DirectoryFilter: dir => !settings.ExcludePatterns.Any(re => re.IsMatch(dir.Name))
    );

    // search files sequence
    var fileList = searchDir.SelectFilesAsync(options: options, selector: async context =>
    {
        using var stream = context.File!.OpenRead();
        var sha1 = await SHA1.HashDataAsync(stream);
        return new
        {
            Path = settings.OutputFullName ? context.File?.FullName : context.File?.RelativePathFrom(searchDir),
            Size = context.File?.Length,
            LastWrite = context.File?.LastAccessTime,
            SHA1 = sha1.ToHexString(),
        };
    });

    // Save results to file
    var outputFile = ThisSource.RelativeFile($"files-{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    if (settings.FormatExcel)
    {
        await fileList.SaveToExcelAsync(outputFile);
    }
    else
    {
        await fileList.SaveToCsvAsync(outputFile);
    }

});
