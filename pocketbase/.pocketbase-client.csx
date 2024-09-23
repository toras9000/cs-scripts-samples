#r "nuget: WebSerializer, 1.3.0"
#nullable enable
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Cysharp.Web;

public class BaseItem
{
    public string id { get; set; } = default!;
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
}

public record AdminAuthArgs(string identity, string password);
public record AdminAuthResult(string token, AdminAuthItem admin);
public class AdminAuthItem : BaseItem
{
    public string email { get; set; } = default!;
    public int avatar { get; set; }
}

public record ListArgs(int? page = default, int? perPage = default, string? sort = default, string? filter = default, string? fields = default, bool? skipTotal = default);
public record ListResult<TItem>(int page, int perPage, int totalItems, int totalPages, TItem[] items);

public record QueryArgs(int? expand = default, string? fields = default);
public record CreateArgs(int? page = default, int? perPage = default, string? sort = default, string? filter = default, string? fields = default, bool? skipTotal = default);

public class CollectionsItem : BaseItem
{
    public string name { get; set; } = default!;
    public string type { get; set; } = default!;
    public bool system { get; set; }
    public CollectionsSchema[]? schema { get; set; }
    public string? listRule { get; set; }
    public string? viewRule { get; set; }
    public string? createRule { get; set; }
    public string? updateRule { get; set; }
    public string? deleteRule { get; set; }
    public Dictionary<string, object> options { get; set; } = default!;
    public string[]? indexes { get; set; }
}
public class CollectionsSchema
{
    public bool system { get; set; }
    public string id { get; set; } = default!;
    public string name { get; set; } = default!;
    public string type { get; set; } = default!;
    public bool required { get; set; }
    public bool unique { get; set; }
    public Dictionary<string, object> options { get; set; } = default!;
}

public class RecordsItem : BaseItem
{
    public string collectionId { get; set; } = default!;
    public string collectionName { get; set; } = default!;
}

public class UserItem : RecordsItem
{
    public string username { get; set; } = default!;
    public string email { get; set; } = default!;
    public bool emailVisibility { get; set; }
    public bool verified { get; set; }
}

public class PocketBaseClient : IDisposable
{
    public PocketBaseClient(Uri service)
    {
        this.handler = new AuthMessageHandler(this) { InnerHandler = new HttpClientHandler(), };
        this.http = new HttpClient(this.handler);
        this.reqJsonOpt = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new DateTimeConverter("yyyy-MM-dd HH:mm:ss.fffK"),
            }
        };
        this.rspJsonOpt = new()
        {
            Converters =
            {
                new DateTimeConverter("yyyy-MM-dd HH:mm:ss.fffK"),
            }
        };

        this.Service = service;
        this.Admins = new(this);
        this.Collections = new(this);
        this.Records = new(this);
    }

    public class AdminsApi(PocketBaseClient outer)
    {
        public Task<AdminAuthResult> AuthWithPsswordAsync(AdminAuthArgs args, CancellationToken cancelToken = default)
            => outer.apiPostAsync<AdminAuthResult>(outer.apiEndpoint("/api/admins/auth-with-password"), args, cancelToken);
    }

    public class CollectionsApi(PocketBaseClient outer)
    {
        public Task<ListResult<CollectionsItem>> ListAsync(ListArgs args, CancellationToken cancelToken = default)
            => outer.apiGetAsync<ListResult<CollectionsItem>>(outer.apiEndpoint("/api/collections", args), cancelToken);
    }

    public class RecordsApi(PocketBaseClient outer)
    {
        public Task<ListResult<TItem>> ListAsync<TItem>(string collection, ListArgs args, CancellationToken cancelToken = default) where TItem : RecordsItem
            => outer.apiGetAsync<ListResult<TItem>>(outer.apiEndpoint($"/api/collections/{collection}/records", args), cancelToken);

        public Task<TItem> ViewAsync<TItem>(string collection, string record, QueryArgs query, CancellationToken cancelToken = default) where TItem : RecordsItem
            => outer.apiGetAsync<TItem>(outer.apiEndpoint($"/api/collections/{collection}/records/{record}", query), cancelToken);

        public Task<TItem> CreateAsync<TItem>(string collection, object args, QueryArgs? query = default, CancellationToken cancelToken = default) where TItem : RecordsItem
            => outer.apiPostAsync<TItem>(outer.apiEndpoint($"/api/collections/{collection}/records", query), args, cancelToken);

        public Task<TItem> UpdateAsync<TItem>(string collection, string record, object args, QueryArgs? query = default, CancellationToken cancelToken = default) where TItem : RecordsItem
            => outer.apiPatchAsync<TItem>(outer.apiEndpoint($"/api/collections/{collection}/records/{record}", query), args, cancelToken);
    }


    public Uri Service { get; }

    public AdminsApi Admins { get; }
    public CollectionsApi Collections { get; }
    public RecordsApi Records { get; }

    public void SetAccessToken(string? token)
    {
        this.apiToken = token;
    }

    public void Dispose()
    {
        this.http.Dispose();
    }

    private HttpClient http;
    private HttpMessageHandler handler;
    private JsonSerializerOptions reqJsonOpt;
    private JsonSerializerOptions rspJsonOpt;
    private string? apiToken;

    private class AuthMessageHandler(PocketBaseClient outer) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (outer.apiToken != null) request.Headers.Authorization = new AuthenticationHeaderValue(outer.apiToken);
            return base.SendAsync(request, cancellationToken);
        }
    }


    private class DateTimeConverter(string format) : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.ParseExact(reader.GetString()!, format, CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(format));
    }

    private Uri apiEndpoint(string path, object? query = default)
    {
        if (query == null)
        {
            return new(this.Service, path);
        }

        return new(this.Service, WebSerializer.ToQueryString(path, query));
    }

    private async Task<TResult> apiGetAsync<TResult>(Uri endpoint, CancellationToken cancelToken)
    {
        var response = await this.http.GetAsync(endpoint, cancelToken);
        var success = response.EnsureSuccessStatusCode();
        return await success.Content.ReadFromJsonAsync<TResult>(this.rspJsonOpt, cancelToken) ?? throw new Exception("Invalid response");
    }

    private async Task<TResult> apiPostAsync<TResult>(Uri endpoint, object args, CancellationToken cancelToken)
    {
        var response = await this.http.PostAsJsonAsync(endpoint, args, this.reqJsonOpt, cancelToken);
        var success = response.EnsureSuccessStatusCode();
        return await success.Content.ReadFromJsonAsync<TResult>(this.rspJsonOpt, cancelToken) ?? throw new Exception("Invalid response");
    }

    private async Task<TResult> apiPatchAsync<TResult>(Uri endpoint, object args, CancellationToken cancelToken)
    {
        var response = await this.http.PatchAsJsonAsync(endpoint, args, this.reqJsonOpt, cancelToken);
        var success = response.EnsureSuccessStatusCode();
        return await success.Content.ReadFromJsonAsync<TResult>(this.rspJsonOpt, cancelToken) ?? throw new Exception("Invalid response");
    }

}
