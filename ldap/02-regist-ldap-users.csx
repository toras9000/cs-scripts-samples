#r "nuget: System.DirectoryServices, 8.0.0"
#r "nuget: System.DirectoryServices.Protocols, 8.0.0"
#r "nuget: Lestaly, 0.61.0"
#r "nuget: Kokuban, 0.2.0"
#load ".directory-service-extensions.csx"
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

        // Bind user credential
        BindCredential = new NetworkCredential("uid=configurator,ou=users,dc=myserver,o=home", "configurator-pass"),
    },

    // LDAP directory info
    Directory = new
    {
        // DN to manage users
        UsersDn = "ou=persons,ou=accounts,dc=myserver,o=home",

        // RDN attribute
        RdnAttr = "uid",

        // Search scope for registered confirmation
        Scope = SearchScope.OneLevel,

        // Filter for registered confirmation
        Filter = default(string),
    },
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Read the list of users to be registered.
    WriteLine("Read user list");
    var userListFile = ThisSource.RelativeFile("02-regist-ldap-users-data.csv");
    var userList = userListFile.ReadUserListCsv().ToArray();

    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.Credential = settings.Server.BindCredential;
    ldap.Bind();

    // Search for existing users
    var registered = await ldap.SearchUsersAsync(
        settings.Directory.UsersDn,
        settings.Directory.Scope,
        settings.Directory.Filter
    );

    // Registering users on the list
    WriteLine("Regist users");
    foreach (var entry in userList)
    {
        // User DN
        var userDn = $"{settings.Directory.RdnAttr}={entry.UserName},{settings.Directory.UsersDn}";

        // Processing according to registration status
        var existUser = registered.FirstOrDefault(e => e.Info.UserName == entry.UserName);
        if (existUser == null)
        {
            // Not registered, add it.
            WriteLine($"User regist: {entry.UserName}");
            var registUser = new AddRequest();
            registUser.DistinguishedName = userDn;
            registUser.Attributes.Add(new("objectClass", ["inetOrgPerson", "extensibleObject"]));
            registUser.Attributes.Add(new("uidNumber", entry.ID.ToString()));
            registUser.Attributes.Add(new("uid", entry.UserName));
            registUser.Attributes.Add(new("cn", $"{entry.GivenName} {entry.SurName}"));
            registUser.Attributes.Add(new("sn", entry.SurName));          // surname
            registUser.Attributes.Add(new("gn", entry.GivenName));        // givenName
            registUser.Attributes.Add(new("displayName", entry.DisplayName));
            registUser.Attributes.Add(new("mail", entry.Mail));

            // Request add
            var registResult = await ldap.SendRequestAsync(registUser);
            if (registResult.ResultCode != ResultCode.Success) throw new PavedMessageException($"Failed: Code={registResult.ResultCode},{registResult.ErrorMessage}");
            WriteLine($"  Successful");
        }
        else
        {
            // Attempt to update if registered
            WriteLine($"User exist: {entry.UserName}");
            if (existUser.DistinguishedName != userDn) throw new PavedMessageException($"Unexpected DN");
            if (existUser.Info.ID != entry.ID) throw new PavedMessageException($"Unexpected ID");

            // Determine if each piece of information needs to be updated.
            var updateSurName = existUser.Info.SurName != entry.SurName;
            var updateGivenName = existUser.Info.GivenName != entry.GivenName;
            var updateDispName = existUser.Info.DisplayName != entry.DisplayName;
            var updateMail = existUser.Info.SurName != entry.SurName;

            // Determine if there is information that needs to be updated.
            var needUpdate = updateSurName || updateGivenName || updateDispName || updateMail;
            if (needUpdate)
            {
                // Create update request
                var updateUser = new ModifyRequest();
                updateUser.DistinguishedName = userDn;
                if (updateSurName) updateUser.AddAttributeReplace("sn", entry.SurName);
                if (updateGivenName) updateUser.AddAttributeReplace("gn", entry.GivenName);
                if (updateDispName) updateUser.AddAttributeReplace("displayName", entry.DisplayName);
                if (updateMail) updateUser.AddAttributeReplace("mail", entry.Mail);
                if (updateSurName || updateGivenName) updateUser.AddAttributeReplace("cn", $"{entry.GivenName} {entry.SurName}");

                // Request update
                var updateResult = await ldap.SendRequestAsync(updateUser);
                if (updateResult.ResultCode != ResultCode.Success) throw new PavedMessageException($"Failed: Code={updateResult.ResultCode},{updateResult.ErrorMessage}");
                WriteLine($"  Successful");
            }
            else
            {
                WriteLine($"  Skip: no changes.");
            }
        }
    }

});

// User information from LDAP
public record LdapUserEntry(string DistinguishedName, string CommonName, UserEntry Info);

// User Information Retrieval from LDAP
static async ValueTask<LdapUserEntry[]> SearchUsersAsync(this LdapConnection self, string dn, SearchScope scope = SearchScope.OneLevel, string? filter = null)
{
    // Create a search request.
    var searchReq = new SearchRequest();
    searchReq.DistinguishedName = dn;
    searchReq.Scope = scope;
    if (filter.IsNotEmpty()) searchReq.Filter = filter;

    // Request a search.
    var searchRsp = await self.SendRequestAsync(searchReq);
    var searchResult = searchRsp as SearchResponse ?? throw new PavedMessageException("no data");

    // Obtain user information from the results.
    var expectClasses = new[] { "inetOrgPerson", "extensibleObject", };
    var resultList = new List<LdapUserEntry>();
    foreach (var entry in searchResult.Entries.OfType<SearchResultEntry>())
    {
        // It must contain the classes assumed.
        if (entry.EnumerateAttributeValues("objectClass").Intersect(expectClasses).Count() != expectClasses.Length) throw new PavedMessageException("Unexpected objectClass");

        //  Retrieving User Information
        var number = entry.GetAttributeSingleValue("uidNumber")?.TryParseNumber<int>() ?? throw new PavedMessageException("Unexpected uidNumber");
        var username = entry.GetAttributeSingleValue("uid") ?? throw new PavedMessageException("Unexpected uid");
        var surname = entry.GetAttributeSingleValue("sn") ?? throw new PavedMessageException("Unexpected sn");
        var givenname = entry.GetAttributeSingleValue("givenName") ?? throw new PavedMessageException("Unexpected givenName");
        var dispname = entry.GetAttributeSingleValue("displayName") ?? throw new PavedMessageException("Unexpected displayName");
        var mail = entry.GetAttributeSingleValue("mail") ?? throw new PavedMessageException("Unexpected mail");
        var commonname = entry.GetAttributeSingleValue("cn") ?? throw new PavedMessageException("Unexpected cn");

        // Create and collect user information types.
        var user = new UserEntry(number, username, surname, givenname, dispname, mail);
        var ldapEntry = new LdapUserEntry(entry.DistinguishedName, commonname, user);
        resultList.Add(ldapEntry);
    }

    return resultList.ToArray();
}
