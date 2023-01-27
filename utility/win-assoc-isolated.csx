#nullable enable
#r "nuget: Lestaly, 0.28.0"
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Lestaly;

// Add a isolated assembly load context option for dotnet-script associations on Windows.
// It is assumed to be run in the presence of an association entry created by `dotnet-script register`.

return await Paved.RunAsync(configuration: o => o.AnyPause(), action: async () =>
{
    // dummy
    await Task.CompletedTask;

    // Check platform type.
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        throw new PavedMessageException("This script is only available for Windows.");
    }

    // Get the associated command line key.
    var assocKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\dotnetscript\Shell\Open\Command", writable: true);
    if (assocKey == null) throw new PavedMessageException("Association key not found. Possibly `dotnet-script register` is not applied.");

    // Obtains the specified value of the key.
    var cmdLine = assocKey.GetValue(null, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    if (cmdLine == null) throw new PavedMessageException("Unable to obtain the association command line.");

    // Get command line.
    var cmdMatch = cmdLine.Match(@"^\s*""%ProgramFiles%\\dotnet\\dotnet.exe""\s+script\s+(--isolated-load-context\s+)?""%1""\s+--\s+%\*\s*$", RegexOptions.IgnoreCase);
    if (!cmdMatch.Success) throw new PavedMessageException("The association command line is not what it is supposed to be.");

    // Check if it has already been set up.
    if (!cmdMatch.Groups[1].ValueSpan.IsEmpty) throw new PavedMessageException("The isolated context option is already set.", PavedMessageKind.Warning);

    // Rewrite the registry.
    assocKey.SetValue(null, @"""%ProgramFiles%\dotnet\dotnet.exe"" script --isolated-load-context ""%1"" -- %*", RegistryValueKind.ExpandString);

    // Output of normal completion.
    ConsoleWig.WriteLineColord(ConsoleColor.Green, "The registry has been updated.");
});
