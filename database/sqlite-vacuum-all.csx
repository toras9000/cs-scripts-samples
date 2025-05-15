#r "nuget: Lestaly, 0.81.0"
#r "nuget: System.Data.SQLite.Core, 1.0.119"
#r "nuget: Dapper, 2.1.35"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Data.SQLite;
using Dapper;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    // Console-related preparations
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Enter scan path
    WriteLine();
    WriteLine("input scan directory");
    Write(">");
    var input = ReadLine()?.Unquote();
    if (input.IsWhite()) return;

    var dbExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    dbExtensions.Add(".db");
    dbExtensions.Add(".sqlite");

    var scanDir = CurrentDir.RelativeDirectory(input);
    foreach (var file in scanDir.EnumerateFiles("*", SearchOption.AllDirectories))
    {
        // 
        if (!dbExtensions.Contains(file.Extension)) continue;

        try
        {
            WriteLine(Chalk.Green[$"File: {file.FullName}"]);
            var builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = file.FullName;
            builder.Pooling = false;

            using (var db = new SQLiteConnection(builder.ConnectionString))
            {
                await db.OpenAsync();
                await db.ExecuteAsync("vacuum", commandType: System.Data.CommandType.Text);
                await db.ExecuteAsync("reindex", commandType: System.Data.CommandType.Text);
            }

            WriteLine(Chalk.Green[$"  Success"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"  Error: {ex.Message}"]);
        }
    }

});
