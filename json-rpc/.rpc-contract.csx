#nullable enable

public interface IMemoryService : IDisposable
{
    Task<KeyValuePair<string, string>[]> GetListAsync();
    Task<string?> GetEntryAsync(string key);
    Task<bool> SetEntryAsync(string key, string? value);
}
