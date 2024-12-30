#r "nuget: System.DirectoryServices, 9.0.0"
#r "nuget: System.DirectoryServices.Protocols, 9.0.0"
#r "nuget: Lestaly, 0.69.0"
#r "nuget: Kokuban, 0.2.0"
#load ".directory-service-extensions.csx"
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
        BindCredential = new NetworkCredential("uid=authenticator,ou=users,dc=myserver,o=home", "authenticator-pass"),
    },

    // Search option
    Search = new
    {
        FixedTargetDn = "",
    },
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
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
    var targetDn = settings.Search.FixedTargetDn.WhenWhite(ConsoleWig.NewLine().WriteLine("Enter the base DN for the search.").Write(">").ReadLine().CancelIfWhite());

    // Current Search Scope. Changes with search input.
    var scope = SearchScope.OneLevel;

    // Interpret scopes and filters.
    static string interpretFilter(string text, ref SearchScope scope)
    {
        var source = text.AsSpan().TrimStart();
        if (source.StartsWithAnyIgnoreCase(["base:"]))
        {
            scope = SearchScope.Base;
            return source.Slice(source.IndexOf(':') + 1).ToString();
        }
        else if (source.StartsWithAnyIgnoreCase(["one:", "onelv:", "onelevel:"]))
        {
            scope = SearchScope.OneLevel;
            return source.Slice(source.IndexOf(':') + 1).ToString();
        }
        else if (source.StartsWithAnyIgnoreCase(["sub:", "subtree:", "tree:"]))
        {
            scope = SearchScope.Subtree;
            return source.Slice(source.IndexOf(':') + 1).ToString();
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
            var request = new SearchRequest();
            request.DistinguishedName = targetDn;
            request.Scope = scope;
            request.Filter = filter;
            var response = await ldap.SendRequestAsync(request);
            if (response.ResultCode != 0) throw new PavedMessageException($"failed to request, msg={response.ErrorMessage}");
            var searchResult = response as SearchResponse ?? throw new PavedMessageException("not expected result");

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
