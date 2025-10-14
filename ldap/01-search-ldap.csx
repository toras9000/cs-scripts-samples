#r "nuget: System.DirectoryServices, 9.0.10"
#r "nuget: System.DirectoryServices.Protocols, 9.0.10"
#r "nuget: Lestaly.General, 0.106.0"
#r "nuget: Lestaly.Ldap, 0.101.0"
#r "nuget: Kokuban, 0.2.0"
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

        // Bind user credential, null is anonymous
        BindCredential = new NetworkCredential("uid=authenticator,ou=operators,dc=myserver,o=home", "authenticator-pass"),
    },

    // Search option
    Search = new
    {
        FixedTargetDn = "",
    },
};

return await Paved.ProceedAsync(async () =>
{
    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
    if (settings.Server.BindCredential == null)
    {
        ldap.AuthType = AuthType.Anonymous;
    }
    else
    {
        ldap.AuthType = AuthType.Basic;
        ldap.Credential = settings.Server.BindCredential;
    }
    ldap.Bind();

    // Ask the user to enter a search target.
    WriteLine(); WriteLine("Enter the base DN for the search."); Write(">");
    var targetDn = settings.Search.FixedTargetDn.WhenWhite(ReadLine().CancelIfWhite());

    // Current Search Scope. Changes with search input.
    var scope = SearchScope.OneLevel;

    // Interpret scopes and filters.
    static string interpretFilter(string text, ref SearchScope scope)
    {
        var source = text.AsSpan().TrimStart();
        var next = 0;
        if (source.RoughStartsAny(["base:"], out next))
        {
            scope = SearchScope.Base;
            return source[next..].ToString();
        }
        else if (source.RoughStartsAny(["one:", "onelv:", "onelevel:"], out next))
        {
            scope = SearchScope.OneLevel;
            return source[next..].ToString();
        }
        else if (source.RoughStartsAny(["sub:", "subtree:", "tree:"], out next))
        {
            scope = SearchScope.Subtree;
            return source[next..].ToString();
        }

        return text;
    }

    // Search for input.
    WriteLine(); WriteLine("Input search filter");
    while (true)
    {
        // Read input.
        Write(">");
        var input = ReadLine();
        if (input.IsWhite()) break;

        // Interpret scopes and filters.
        var filter = interpretFilter(input, ref scope);
        if (filter.IsWhite()) continue;

        try
        {
            // Perform a search request.
            var searchResult = await ldap.SearchAsync(targetDn, scope, filter);

            // Show results
            if (searchResult.Entries.Count <= 0)
            {
                WriteLine(Chalk.Yellow["no results"]);
            }
            else
            {
                foreach (var entry in searchResult.Entries.OfType<SearchResultEntry>())
                {
                    WriteLine(Chalk.Green[entry.DistinguishedName]);
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
        }
        WriteLine();
    }

});
