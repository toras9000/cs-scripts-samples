#r "nuget: Lestaly, 0.52.0"
#r "nuget: System.Data.SQLite.Core, 1.0.118"
#nullable enable
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using CommandLine;
using Lestaly;
using System.Data.SQLite;

return await Paved.RunAsync(async () =>
{
    // Console-related preparations
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    while (true)
    {
        // Enter database path
        ConsoleWig.WriteLine();
        ConsoleWig.WriteLine("input SQLite database file path.(drag&drop)");
        var input = await ConsoleWig.Write(">").ReadKeysLineIfAsync(t => t.Unquote().EndsWithAny(new[] { ".db", ".sqlite", }));
        if (input.IsWhite()) break;

        // Existence check of the specified file
        var dbFile = CurrentDir.RelativeFile(input.Unquote());
        if (!dbFile.Exists)
        {
            Console.WriteLine($"Not found '{dbFile.FullName}'");
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

                // Output records
                while (reader.Read())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (i != 0) writer.Write(',');
                        writer.Write(reader.GetValue(i));
                    }
                    writer.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleWig.WriteLineColored(ConsoleColor.Red, ex.Message);
        }
    }

});
