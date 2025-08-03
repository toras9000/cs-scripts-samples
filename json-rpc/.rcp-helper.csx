#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Kokuban, 0.2.0"
#load ".rpc-contract.csx"
#nullable enable
using System.Threading;
using Kokuban;
using Lestaly;

static async Task InterpretCommandsAsync(IMemoryService memory, CancellationToken cancelToken)
{
    var delimiters = new char[] { ' ', '\t' };
    while (true)
    {
        Write(">");
        var input = ReadLine();
        if (input == null) break;
        if (input.IsWhite()) continue;

        try
        {
            var cmd = input.TrimStart(delimiters).TakeSkipTokenAny(out var args, delimiters);
            if (cmd.RoughAny(["help", "?"]))
            {
                WriteLine($"Command list");
                WriteLine($"  get <key>");
                WriteLine($"  set <key> <value>");
                WriteLine($"  list");
            }
            else if (cmd.RoughAny(["get"]))
            {
                var key = args.TrimStart(delimiters).TakeSkipTokenAny(out args, delimiters).ThrowIfWhite(() => new Exception("Parameter missing")).ToString();
                var value = await memory.GetEntryAsync(key);
                WriteLine((value == null) ? $"no entry" : $"{key} = {value}");
            }
            else if (cmd.RoughAny(["set"]))
            {
                var key = args.TrimStart(delimiters).TakeSkipTokenAny(out args, delimiters).ThrowIfWhite(() => new Exception("Parameter missing")).ToString();
                var value = args.TrimStart(delimiters).TakeSkipTokenAny(out args, delimiters).ThrowIfWhite(() => new Exception("Parameter missing")).ToString();
                await memory.SetEntryAsync(key, value);
                WriteLine($"{key} = {value}");
            }
            else if (cmd.RoughAny(["list"]))
            {
                var list = await memory.GetListAsync();
                foreach (var (key, value) in list)
                {
                    WriteLine($"{key} = {value}");
                }
                if (list.Length <= 0) WriteLine($"no entry");
            }
            else
            {
                throw new Exception("Unknown command");
            }
        }
        catch (Exception ex) when (!cancelToken.IsCancellationRequested)
        {
            WriteLine(Chalk.Yellow[ex.Message]);
        }
    }
}

public static ReadOnlySpan<char> ThrowIfWhite(this ReadOnlySpan<char> self, Func<Exception>? generator = null)
    => self.IsEmpty ? throw generator?.Invoke() ?? new InvalidDataException() : self;
