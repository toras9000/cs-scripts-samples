#r "nuget: Lestaly, 0.69.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.RunAsync(config: c => c.AnyPause(), action: async () =>
{
    async ValueTask callArgTestAsync(params string[] parameters)
    {
        WriteLine($"Call Params: {parameters.JoinString(" ")}");
        await "dotnet".args(["script", ThisSource.RelativeFile("args-set.csx").FullName, "--", .. parameters]);
        WriteLine();
    }

    await callArgTestAsync("aaa");
    await callArgTestAsync("aaa", "-o", "OPTSTR");
    await callArgTestAsync("aaa", "-d", "A", "-d", "B", "-d", "C");
    await callArgTestAsync("aaa", "-l", "X", "-l", "Y", "-l", "Z");
    await callArgTestAsync("aaa", "-f");
    await callArgTestAsync("aaa", "-o", "A", "-d", "B", "-d", "C", "-l", "D", "-f");
    await callArgTestAsync();

});
