#r "nuget: ClosedXML, 0.104.0-preview2"
#r "nuget: Lestaly, 0.56.0"
#r "nuget: Kokuban, 0.2.0"
using Lestaly;
using Kokuban;
using ClosedXML.Excel;

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    await Task.CompletedTask;

    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    while (true)
    {
        try
        {
            var inputPath = ConsoleWig.Write("Read excel file\n>").ReadLine().CancelIfWhite();
            var inputFile = CurrentDir.RelativeFile(inputPath);
            Console.WriteLine();

            using var book = new XLWorkbook(inputFile.FullName);
            Console.WriteLine(Chalk.Green[$"Worksheets in book: {inputFile.FullName}"]);
            foreach (var sheet in book.Worksheets)
            {
                Console.WriteLine($"  {sheet.Name}");
            }
            Console.WriteLine();

            var first = book.Worksheets.First();
            Console.WriteLine(Chalk.Green[$"Some cells in sheet: {first.Name}"]);
            var used = first.RangeUsed();
            var usedFirstCol = used.FirstColumn().ColumnNumber();
            var usedFirstRow = used.FirstRow().RowNumber();
            var numWidth = 4;
            var colWidth = 14;
            var showRows = Math.Min(used.RowCount(), 10);
            var showCols = Math.Min(used.ColumnCount(), 6);
            var colLetters = Enumerable.Range(0, showCols).Select(n => XLHelper.GetColumnLetterFromNumber(usedFirstCol + n));
            Console.WriteLine(colLetters.Select(c => c.PadRight(colWidth)).Prepend("".PadLeft(numWidth)).JoinString(" "));
            for (var rowIdx = 0; rowIdx < showRows; rowIdx++)
            {
                var row = used.Row(1 + rowIdx);
                var rowNum = row.RowNumber().ToString().PadLeft(numWidth);
                Console.Write($"{rowNum} ");
                for (var colIdx = 0; colIdx < showCols; colIdx++)
                {
                    var cellText = row.Cell(1 + colIdx).GetFormattedString();
                    var showText = cellText.EllipsisByWidth(colWidth).PadRight(colWidth);
                    Console.Write($"{showText} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
        }
    }
});
