#r "nuget: Lestaly.General, 0.105.0"
#r "nuget: System.Data.SQLite.Core, 1.0.119"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Data;
using System.Data.SQLite;
using System.Reflection;
using System.Text.RegularExpressions;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    // Console-related preparations
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    while (true)
    {
        // Enter database path
        WriteLine();
        WriteLine("input SQLite database file path.(drag&drop)");
        Write(">");
        var input = ReadLine().Unquote();
        if (input == null) break;
        if (input.IsWhite()) continue;

        // Existence check of the specified file
        var dbFile = CurrentDir.RelativeFile(input);
        if (!dbFile.Exists)
        {
            WriteLine($"Not found '{dbFile.FullName}'");
            continue;
        }

        try
        {
            // Output directory
            var exportDir = dbFile.RelativeDirectory($"{dbFile.Name}-{DateTime.Now:yyyyMMdd-HHmmss}");

            // Databese connection configuration
            var config = new SQLiteConnectionStringBuilder();
            config.DataSource = dbFile.FullName;
            config.FailIfMissing = true;
            config.ReadOnly = true;
            config.ForeignKeys = true;
            config.Pooling = false;

            // Open database
            using var db = new SQLiteConnection(config.ConnectionString);
            await db.OpenAsync();

            // Get table list
            var tables = await db.GetSchemaAsync("Tables", signal.Token);
            foreach (var row in tables.AsEnumerable())
            {
                // Table name
                var name = row.Field<string>("TABLE_NAME");

                // Get all records of table command
                using var cmd = db.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $"select * from {name}";

                // Create a reader from queries
                using var reader = await cmd.ExecuteReaderAsync();

                // Create a writer to the export destination file.
                using var writer = exportDir.RelativeFile($"{name}.csv").WithDirectoryCreate().CreateTextWriter();

                // Output header
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    if (i != 0) writer.Write(',');
                    writer.Write(reader.GetName(i));
                }
                writer.WriteLine();

                // Local function for Quote if need
                static string? csvField(object? value)
                {
                    var str = value?.ToString();
                    if (str == null) return default;
                    if (str.IndexOfAny([',', '"']) < 0) return str;
                    return str.Quote();
                }

                // Output records
                while (reader.Read())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (i != 0) writer.Write(',');
                        writer.Write(csvField(reader.GetValue(i)));
                    }
                    writer.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[ex.Message]);
        }
    }

});
