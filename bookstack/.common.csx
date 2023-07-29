#r "nuget: BookStackApiClient, 23.6.1"
#r "nuget: SkiaSharp, 2.88.3"
#r "nuget: Lestaly, 0.43.0"
#nullable enable
using System.Threading;
using BookStackApiClient;
using Lestaly;

/// <summary>API Access Information</summary>
/// <param name="ApiEntry">API base address</param>
/// <param name="Token">API Token ID</param>
/// <param name="Secret">API Token Secret</param>
public record ApiKeyInfo(Uri ApiEntry, string Token, string Secret);

/// <summary>
/// API Key Storage Management
/// </summary>
public class ApiKeyStore
{
    /// <summary>API key scramble save file.</summary>
    public static FileInfo ScrambleFile { get; } = ThisSource.RelativeFile(".bookstack-api-key.sav");

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
        var token = ConsoleWig.Write("API Token\n>").ReadLine();
        if (token.IsWhite()) throw new OperationCanceledException();
        var secret = ConsoleWig.Write("API Secret\n>").ReadLine();
        if (secret.IsWhite()) throw new OperationCanceledException();
        keyInfo = new(apiEntry, token, secret);
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

/// <summary>
/// Auxiliary class for BookStackClient
/// </summary>
public class BookStackClientHelper
{
    /// <summary>Constructor that ties the client instance.</summary>
    /// <param name="client">BookStackClient instance</param>
    public BookStackClientHelper(BookStackClient client)
    {
        this.client = client;
    }

    /// <summary>Helper method to retry at API request limit</summary>
    /// <param name="accessor">API request processing</param>
    /// <typeparam name="TResult">API return type</typeparam>
    /// <returns>API return value</returns>
    public async ValueTask<TResult> Try<TResult>(Func<BookStackClient, Task<TResult>> accessor)
    {
        while (true)
        {
            try
            {
                return await accessor(this.client).ConfigureAwait(true);
            }
            catch (ApiLimitResponseException ex)
            {
                ConsoleWig.WriteLineColored(ConsoleColor.Red, $"API request rate limit reached. Rate limit: {ex.RequestsPerMin} [per minute]");
                ConsoleWig.WriteLineColored(ConsoleColor.Yellow, $"Automatically retry after a while. If you press any key, an early retry is performed.");
                ConsoleWig.WriteColored(ConsoleColor.Yellow, $"[Waiting...]");

                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < (ex.RetryAfter * 1000))
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(intercept: true);
                        break;
                    }
                    await Task.Delay(50).ConfigureAwait(true);
                }
                Console.WriteLine();
            }
        }
    }

    /// <summary>Helper method to retry at API request limit</summary>
    /// <param name="accessor">API request processing</param>
    public async ValueTask Try(Func<BookStackClient, Task> accessor)
    {
        await Try<int>(async c => { await accessor(c); return 0; });
    }

    /// <summary>client instance</summary>
    private BookStackClient client;
}
