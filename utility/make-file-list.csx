#r "nuget: Lestaly, 0.81.0"
#r "nuget: Cocona.Lite, 2.2.0"
#nullable enable
using System.Reflection;
using System.Text.RegularExpressions;
using Cocona;
using Lestaly;

// Command line argument mapping type
class Options : ICommandParameterSet
{
    [Argument(Order = 0), Option(Description = "Search target directory"), HasDefaultValue]
    public string? Target { get; set; }

    [Option("outdir", ['o'], Description = "Output directory."), HasDefaultValue]
    public string? OutDir { get; set; }

    [Option("excludes", ['e'], Description = "Exclusion patterns from search. (regex)"), HasDefaultValue]
    public string[] ExcludePatterns { get; set; } = ["^.git", "^.hg", "^.svn"];

    [Option("fullname", ['f'], Description = "Flag to output full path.")]
    public bool FullName { get; set; }

    [Option("excel", ['l'], Description = "Flag to output as Excel file.")]
    public bool FormatExcel { get; set; }

    [Option('t', Description = "Flag to output timestamps.")]
    public bool WithTime { get; set; }

    [Option('x', Description = "Flag to output extension.")]
    public bool WithExt { get; set; }

    [Option('s', Description = "Flag to output size.")]
    public bool WithSize { get; set; }
}

// Data type for information collection and output
record FileItem(string Path, string Extension, DateTime LastWrite, long Size);

await CoconaLiteApp.RunAsync(args: Args.ToArray(), commandBody: async (Options options) =>
{
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
            var time = options.WithTime ? file.LastAccessTime : default;
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
