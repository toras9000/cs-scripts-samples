#r "nuget: Lestaly, 0.69.0"
#load ".pocketbase-client.csx"
#nullable enable
using System.Threading;
using Lestaly;

var settings = new
{
    Url = new Uri("http://127.0.0.1:8090"),

    Admin = new
    {
        ID = "admin@example.com",
        Pass = "admin-pass",
    },
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    using var client = new PocketBaseClient(settings.Url);

    var admin = await client.Admins.AuthWithPsswordAsync(new(settings.Admin.ID, settings.Admin.Pass));
    client.SetAccessToken(admin.token);
    var users = await client.Records.ListAsync<UserItem>("users", new(perPage: 100));
    foreach (var user in users.items)
    {
        WriteLine($"User: Name={user.username}");
    }
});
