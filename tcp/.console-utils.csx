#r "nuget: Kokuban, 0.2.0"
#nullable enable
using Kokuban;

static class ConsoleUtils
{
    public static void WritePrompt()
    {
        lock (ConsoleSync)
        {
            Console.Write('>');
        }
    }

    public static void WriteAttention(string message)
    {
        lock (ConsoleSync)
        {
            Console.WriteLine(Chalk.Yellow[message]);
        }
    }

    public static void WriteInsertion(string text)
    {
        lock (ConsoleSync)
        {
            var orgCurPos = Console.CursorLeft;
            Console.Write(Environment.NewLine);
            Console.WriteLine(Chalk.Gray[text]);
            Console.Write('>');
            if (2 < orgCurPos)
            {
                Console.Write(new string('.', orgCurPos - 1));
            }
        }
    }

    private static readonly object ConsoleSync = new();
}