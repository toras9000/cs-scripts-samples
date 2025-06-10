#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
#load ".text-helper.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using Kokuban;
using Lestaly;

var settings = new
{
    // LDAP server settings
    Server = new
    {
        // Host name or ip
        Host = "ldap.myserver.home",

        // Port number
        Port = 636,

        // Use SSL
        Ssl = true,

        // LDAP protocol version
        ProtocolVersion = 3,
    },
};

return await Paved.ProceedAsync(async () =>
{
    await Task.CompletedTask;

    while (true)
    {
        Write($"Enter DN:"); var dn = ReadLine();
        Write($"Enter Pass:"); var pass = ReadLine();

        var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
        using var ldap = new LdapConnection(server);
        ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
        ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
        ldap.AuthType = AuthType.Basic;

        try
        {
            ldap.Credential = new NetworkCredential(dn, pass);
            ldap.Bind();
            WriteLine(Chalk.Green[$"Success"]);
        }
        catch
        {
            WriteLine(Chalk.Red[$"Failed to bind"]);
        }
        WriteLine();
    }
});
