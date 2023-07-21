#r "nuget: KallitheaApiClient, 0.7.0.16"
#r "nuget: Lestaly, 0.42.0"
#nullable enable
using System.Threading;
using Lestaly;

/// <summary>API Access Information</summary>
/// <param name="ApiEntry">API base address</param>
/// <param name="Token">API Key</param>
public record ApiKeyInfo(Uri ApiEntry, string Token);

/// <summary>
/// API Key Storage Management
/// </summary>
public class ApiKeyStore
{
    /// <summary>API key scramble save file.</summary>
    public static FileInfo ScrambleFile { get; } = ThisSource.RelativeFile(".kallithea-api-key.sav");

    /// <summary>Scramble save context (key)</summary>
    public static string ScrambleContext { get; } = ThisSource.RelativeDirectory(".").FullName;

    /// <summary>API Key Information</summary>
    public ApiKeyInfo Key { get; }

    /// <summary>API Entry address</summary>
    public Uri ApiEntry => this.Key.ApiEntry;

    /// <summary>Attempt to recover saved API key information.</summary>
    /// <param name="apiEntry">API base address</param>
    /// <param name="cancelToken">cancel token</param>
    /// <returns>API Key Management Instance</returns>
    public static async ValueTask<ApiKeyStore> RestoreAsync(Uri apiEntry, CancellationToken cancelToken = default)
    {
        // Attempt to read the stored API key information.
        var scrambler = new RoughScrambler(context: ScrambleContext);
        var keyInfo = await scrambler.DescrambleObjectFromFileAsync<ApiKeyInfo>(ScrambleFile, cancelToken);
        if (keyInfo != null && keyInfo.ApiEntry.AbsoluteUri == apiEntry.AbsoluteUri)
        {
            return new(keyInfo, scrambler, stored: true);
        }

        // If there is no restoration information, it asks for input.
        var key = ConsoleWig.Write("API key\n>").ReadLine();
        if (key.IsWhite()) throw new OperationCanceledException();
        keyInfo = new(apiEntry, key);
        return new(keyInfo, scrambler, stored: false);
    }

    /// <summary>Save API key information</summary>
    /// <param name="cancelToken"></param>
    /// <returns>Success or failure</returns>
    public async ValueTask<bool> SaveAsync(CancellationToken cancelToken = default)
    {
        var result = true;
        if (!this.stored)
        {
            try
            {
                await scrambler.ScrambleObjectToFileAsync(ScrambleFile, this.Key, cancelToken: cancelToken);
                this.stored = true;
                return true;
            }
            catch { result = false; }
        }
        return result;
    }

    /// <summary>constructor</summary>
    /// <param name="key">API Key Information</param>
    /// <param name="scrambler">Key scrambler</param>
    /// <param name="stored">Is the information stored</param>
    private ApiKeyStore(ApiKeyInfo key, RoughScrambler scrambler, bool stored)
    {
        this.Key = key;
        this.scrambler = scrambler;
        this.stored = stored;
    }

    /// <summary>Key scrambler</summary>
    private RoughScrambler scrambler;

    /// <summary>Is the information stored</summary>
    private bool stored;
}
