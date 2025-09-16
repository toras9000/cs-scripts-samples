#nullable enable
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
{
    var webhookEndpoint = new Uri("http://localhost:9978/webhook-accept");
    var postData = new
    {
        Id = 100,
        Number = 1.23,
        Text = "Test message",
        Object = new
        {

        },
        Array = new[]
        {
            "item1",
            "item2",
        },
    };

    using var client = new HttpClient();
    var json = JsonContent.Create(postData);
    await json.LoadIntoBufferAsync();
    await client.PostAsync(webhookEndpoint, json);
}
