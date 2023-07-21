#nullable enable
#r "nuget: Lestaly, 0.42.0"
using System.Reflection;
using System.Text.RegularExpressions;
using CommandLine;
using Lestaly;

// Command line argument mapping type
class Options
{
    [Value(0, HelpText = "Search target directory")]
    public string? Target { get; set; }

    [Option('o', "outdir", HelpText = "Output directory.")]
    public string? OutDir { get; set; }

    [Option('e', "excludes", HelpText = "Exclusion patterns from search. (regex)", Separator = ':', Default = new[] { "^.git", "^.hg", "^.svn" })]
    public IEnumerable<string?>? ExcludePatterns { get; set; }

    [Option('f', "fullname", HelpText = "Flag to output full path.")]
    public bool FullName { get; set; }

    [Option('t', "with-time", HelpText = "Flag to output timestamps.")]
    public bool WithTime { get; set; }

    [Option('x', "with-ext", HelpText = "Flag to output extension.")]
    public bool WithExt { get; set; }

    [Option('s', "with-size", HelpText = "Flag to output size.")]
    public bool WithSize { get; set; }

    [Option('l', "excel", HelpText = "Flag to output as Excel file.")]
    public bool FormatExcel { get; set; }
}

// Data type for information collection and output
record FileItem(string Path, string Extension, DateTime LastWrite, long Size);

return await Paved.RunAsync(async () =>
{
    // Argument parsing
    var options = CliArgs.Parse<Options>(Args);

    // If the search target is unspecified, let the user enter it.
    var path = options.Target
        .WhenWhite(() => ConsoleWig.Write("Search directory:\n>").ReadLine())
        .WhenWhite(() => throw new OperationCanceledException());

    // Exclusion list
    var excludes = options.ExcludePatterns.CoalesceEmpty().DropEmpty()
        .Select(p => new Regex(p))
        .ToArray();

    // Output File Information
    var outDir = ThisSource.RelativeDirectory(options.OutDir);
    var saveExt = options.FormatExcel ? "xlsx" : "csv";
    var saveFile = outDir.RelativeFile($"files-{DateTime.Now:yyyyMMdd_HHmmss}.{saveExt}");

    // Filter output members (columns)
    var memberFilter = (MemberInfo member) => member.Name switch
    {
        nameof(FileItem.LastWrite) => options.WithTime,
        nameof(FileItem.Extension) => options.WithExt,
        nameof(FileItem.Size) => options.WithSize,
        _ => true,
    };

    // Sequence of file search and output information conversion
    var dir = new DirectoryInfo(path);
    var filelist = dir.SelectFiles(c =>
        {
            var file = c.File!;
            var path = options.FullName ? file!.FullName : file.RelativePathFrom(dir, ignoreCase: true);
            var time = options.WithTime ? file.LastAccessTime : default(DateTime);
            var ext = options.WithExt ? file.Extension : "";
            var size = options.WithSize ? file.Length : 0L;
            return new FileItem(path, ext, time, size);
        }, excludes)
        ;

    // Save results to file
    if (options.FormatExcel)
    {
        filelist.SaveToExcel(saveFile, options: new() { MemberFilter = memberFilter, });
    }
    else
    {
        await filelist.SaveToCsvAsync(saveFile, options: new() { MemberFilter = memberFilter, });
    }

});
