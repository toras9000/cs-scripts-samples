#r "nuget: Lestaly, 0.75.0"
#r "nuget: Cocona.Lite, 2.2.0"
#nullable enable
using Cocona;
using Lestaly;

class Options : ICommandParameterSet
{
    [Argument(Order = 0), Option(Description = "Ordinal string.")]
    public string Ordinal { get; set; } = default!;

    [Option('o', Description = "Optional string."), HasDefaultValue]
    public string? Optional { get; set; }

    [Option('d', Description = "HasDedault list"), HasDefaultValue]
    public string[] HasDefaultList { get; set; } = ["^.git", "^.hg", "^.svn"];

    [Option('l', Description = "Optional list."), HasDefaultValue]
    public string[] OptionalList { get; set; } = default!;

    [Option('f', Description = "Flag.")]
    public bool Flag { get; set; }
}

CoconaLiteApp.Run(args: Args.ToArray(), commandBody: (Options options) =>
{
    WriteLine($"  Ordinal={options.Ordinal}");
    WriteLine($"  Optional={options.Optional}");
    WriteLine($"  HasDefaultList={options.HasDefaultList?.JoinString(",")}");
    WriteLine($"  OptionalList={options.OptionalList?.JoinString(",")}");
    WriteLine($"  Flag={options.Flag}");
});
