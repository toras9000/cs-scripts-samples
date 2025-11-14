#r "nuget: Lestaly.General, 0.109.0"
#r "nuget: Lestaly.Excel, 0.101.0"
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
    OutputFullName = false,

    // Flag to output timestamps.
    WithTime = true,

    // Flag to output extension.
    WithExt = true,

    // Flag to output size.
    WithSize = true,

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
    var fileList = searchDir.SelectFiles(options: options, selector: context => new
    {
        Path = settings.OutputFullName ? context.File?.FullName : context.File?.RelativePathFrom(searchDir),
        Extension = settings.WithExt ? context.File?.Extension : default,
        LastWrite = settings.WithTime ? context.File?.LastAccessTime : default,
        Size = settings.WithSize ? context.File?.Length : default
    });

    // Filter output members (columns)
    var fileItem = fileList.ElementDefault();
    var memberFilter = (MemberInfo member) => member.Name switch
    {
        nameof(fileItem.LastWrite) => settings.WithTime,
        nameof(fileItem.Extension) => settings.WithExt,
        nameof(fileItem.Size) => settings.WithSize,
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
