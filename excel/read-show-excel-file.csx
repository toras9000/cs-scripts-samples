#r "nuget: ClosedXML, 0.105.0"
#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
using ClosedXML.Excel;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    await Task.CompletedTask;

    while (true)
    {
        try
        {
            WriteLine("Read excel file"); Write(">");
            var inputPath = ReadLine().CancelIfWhite();
            var inputFile = CurrentDir.RelativeFile(inputPath);
            WriteLine();

            using var book = new XLWorkbook(inputFile.FullName);
            WriteLine(Chalk.Green[$"Worksheets in book: {inputFile.FullName}"]);
            foreach (var sheet in book.Worksheets)
            {
                WriteLine($"  {sheet.Name}");
            }
            WriteLine();

            var first = book.Worksheets.First();
            WriteLine(Chalk.Green[$"Some cells in sheet: {first.Name}"]);
            var used = first.RangeUsed();
            var usedFirstCol = used.FirstColumn().ColumnNumber();
            var usedFirstRow = used.FirstRow().RowNumber();
            var numWidth = 4;
            var colWidth = 14;
            var showRows = Math.Min(used.RowCount(), 10);
            var showCols = Math.Min(used.ColumnCount(), 6);
            var colLetters = Enumerable.Range(0, showCols).Select(n => XLHelper.GetColumnLetterFromNumber(usedFirstCol + n));
            WriteLine(colLetters.Select(c => c.PadRight(colWidth)).Prepend("".PadLeft(numWidth)).JoinString(" "));
            for (var rowIdx = 0; rowIdx < showRows; rowIdx++)
            {
                var row = used.Row(1 + rowIdx);
                var rowNum = row.RowNumber().ToString().PadLeft(numWidth);
                Write($"{rowNum} ");
                for (var colIdx = 0; colIdx < showCols; colIdx++)
                {
                    var cellText = row.Cell(1 + colIdx).GetFormattedString();
                    var showText = cellText.EllipsisByWidth(colWidth).PadRight(colWidth);
                    Write($"{showText} ");
                }
                WriteLine();
            }
            WriteLine();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
        }
    }
});
