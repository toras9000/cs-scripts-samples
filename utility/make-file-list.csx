#r "nuget: Lestaly.General, 0.104.0"
#r "nuget: Lestaly.Excel, 0.100.0"
#nullable enable
using System.Reflection;
using System.Text.RegularExpressions;
using Lestaly;

// Command line argument mapping type
var settings = new
{
    // Exclusion patterns from search. (regex)
    ExcludePatterns = new Regex[] { new("^.git"), new("^.hg"), new("^.svn"), },

    // Flag to output full path.
    OutputFullName = true,

    // Flag to output timestamps.
    WithTime = true,

    // Flag to output extension.
    WithExt = true,

    // Flag to output size.
    WithSize = true,

    // Flag to output as Excel file.
    FormatExcel = false,
};

// Data type for information collection and output
record FileItem(string? Path, string? Extension, DateTime? LastWrite, long? Size);

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("Search Directory");
    Write(">");
    var searchDir = ReadLine().CancelIfWhite().Unquote().CancelIfWhite().AsDirectoryInfo();

    // search options
    var options = new VisitFilesOptions(
        Recurse: true,
        SkipInaccessible: true,
        Handling: VisitFilesHandling.OnlyFile,
        FileFilter: file => !settings.ExcludePatterns.Any(re => re.IsMatch(file.Name)),
        DirectoryFilter: dir => !settings.ExcludePatterns.Any(re => re.IsMatch(dir.Name))
    );

    // search files sequence
    var fileList = searchDir.SelectFiles(options: options, selector: context => new FileItem(
        Path: settings.OutputFullName ? context.File?.FullName : context.File?.RelativePathFrom(searchDir),
        Extension: settings.WithExt ? context.File?.Extension : default,
        LastWrite: settings.WithTime ? context.File?.LastAccessTime : default,
        Size: settings.WithSize ? context.File?.Length : default
    ));

    // Filter output members (columns)
    var memberFilter = (MemberInfo member) => member.Name switch
    {
        nameof(FileItem.LastWrite) => settings.WithTime,
        nameof(FileItem.Extension) => settings.WithExt,
        nameof(FileItem.Size) => settings.WithSize,
        _ => true,
    };

    // Save results to file
    var outputFile = ThisSource.RelativeFile($"files-{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    if (settings.FormatExcel)
    {
        fileList.SaveToExcel(outputFile, options: new() { MemberFilter = memberFilter, });
    }
    else
    {
        await fileList.SaveToCsvAsync(outputFile, options: new() { MemberFilter = memberFilter, });
    }

});
