#r "nuget: Lestaly, 0.68.0"
#r "nuget: Kokuban, 0.2.0"
#load ".rpc-contract.csx"
#nullable enable
using System.Threading;
using Kokuban;
using Lestaly;

static async Task InterpretCommandsAsync(IMemoryService memory, CancellationToken cancelToken)
{
    var comparer = StringComparer.InvariantCultureIgnoreCase;
    while (true)
    {
        Write(">");
        var input = await ConsoleWig.ReadLineAsync(cancelToken);
        if (input.IsWhite()) continue;

        try
        {
            var fields = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = fields[0];
            if (comparer.Equals(cmd, "help") || comparer.Equals(cmd, "?"))
            {
                WriteLine($"Command list");
                WriteLine($"  get <key>");
                WriteLine($"  set <key> <value>");
                WriteLine($"  list");
            }
            else if (comparer.Equals(cmd, "get"))
            {
                if (fields.Length < 2) throw new Exception("Parameter missing");
                var key = fields[1];
                var value = await memory.GetEntryAsync(key);
                WriteLine((value == null) ? $"no entry" : $"{key} = {value}");
            }
            else if (comparer.Equals(cmd, "set"))
            {
                if (fields.Length < 3) throw new Exception("Parameter missing");
                var key = fields[1];
                var value = fields[2];
                await memory.SetEntryAsync(key, value);
                WriteLine($"{key} = {value}");
            }
            else if (comparer.Equals(cmd, "list"))
            {
                var list = await memory.GetListAsync();
                if (list.Length <= 0)
                {
                    WriteLine($"no entry");
                }
                else
                {
                    foreach (var (key, value) in list)
                    {
                        WriteLine($"{key} = {value}");
                    }
                }
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