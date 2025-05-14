#r "nuget: Lestaly, 0.80.0"
#r "nuget: Kokuban, 0.2.0"
#load ".rpc-contract.csx"
#nullable enable
using System.Threading;
using Kokuban;
using Lestaly;

static async Task InterpretCommandsAsync(IMemoryService memory, CancellationToken cancelToken)
{
    while (true)
    {
        Write(">");
        var input = ReadLine();
        if (input == null) break;
        if (input.IsWhite()) continue;

        try
        {
            var cmd = input.TakeSkipToken(out var args, delimiter: ' ');
            if (cmd.RoughAny(["help", "?"]))
            {
                WriteLine($"Command list");
                WriteLine($"  get <key>");
                WriteLine($"  set <key> <value>");
                WriteLine($"  list");
            }
            else if (cmd.RoughAny(["get"]))
            {
                var key = args.TakeSkipToken(out args).ThrowIfWhite(() => new Exception("Parameter missing")).ToString();
                var value = await memory.GetEntryAsync(key);
                WriteLine((value == null) ? $"no entry" : $"{key} = {value}");
            }
            else if (cmd.RoughAny(["set"]))
            {
                var key = args.TakeSkipToken(out args).ThrowIfWhite(() => new Exception("Parameter missing")).ToString();
                var value = args.TakeSkipToken(out args).ThrowIfWhite(() => new Exception("Parameter missing")).ToString();
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
