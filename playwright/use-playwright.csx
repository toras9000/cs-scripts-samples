#r "nuget: Microsoft.Playwright, 1.52.0"
#r "nuget: Lestaly, 0.81.0"
#nullable enable
using Microsoft.Playwright;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    // dotnet-script does not copy assets. Directly reference the package directory.
    WriteLine("Prepare playwright");
    var packageVer = typeof(Microsoft.Playwright.Program).Assembly.GetName()?.Version?.ToString(3) ?? "*";
    var packageDir = SpecialFolder.UserProfile().FindPathDirectory([".nuget", "packages", "Microsoft.Playwright", packageVer], MatchCasing.CaseInsensitive);
    Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", packageDir?.FullName);
    Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);

    WriteLine("Test page operation");
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = "", });
    var page = await browser.NewPageAsync();
    var response = await page.GotoAsync("https://www.nuget.org/") ?? throw new PavedMessageException("Cannot access page");
    await response.FinishedAsync();
    var serach = page.Locator("input[id='search']");
    await serach.FillAsync("playwright");
    await serach.PressAsync("Enter");
    var packages = await page.Locator("a.package-title").AllAsync();
    foreach (var entry in packages)
    {
        var title = await entry.TextContentAsync();
        WriteLine($"Title={title?.Trim()}");
    }
});
