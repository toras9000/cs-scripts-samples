#r "nuget: Lestaly, 0.73.0"
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

        // Bind user credential, null is anonymous
        BindCredential = new NetworkCredential("cn=config-admin,cn=config", "config-admin-pass"),

        // Configuration Base DN
        ConfigDn = "olcDatabase={2}mdb,cn=config",

        // Access definitions to be added
        AccessDefineFile = ThisSource.RelativeFile("00-config-add-access-data.txt"),
    },

};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Read the access definition to be added
    var accessDefines = settings.Server.AccessDefineFile.EnumerateTextBlocks().ToArray();

    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.Credential = settings.Server.BindCredential;
    ldap.Bind();

    // Read the existing access definition.
    WriteLine("Request a search");
    var configEntry = await ldap.GetEntryAsync(settings.Server.ConfigDn) ?? throw new PavedMessageException("unexpected result");
    var accessExists = configEntry.EnumerateAttributeValues("olcAccess").OfType<string>().ToArray();

    // Definitions that already exist shall be excluded from the addition.
    WriteLine("Detecting changes in access");
    var addAccesses = accessDefines
        .ExceptBy(accessExists.Select(a => a.RemoveIndex().NormalizeSpace()), a => a.NormalizeSpace())
        .ToArray();
    if (addAccesses.Length <= 0)
    {
        WriteLine("... All access definitions already exist.");
        return;
    }

    // Create change information to add attributes.
    WriteLine("Request a change in access.");
    var accessAttr = new DirectoryAttributeModification();
    accessAttr.Operation = DirectoryAttributeOperation.Add;
    accessAttr.Name = "olcAccess";
    foreach (var access in addAccesses)
    {
        accessAttr.Add(access);
    }

    // Request a change
    var accessModify = new ModifyRequest();
    accessModify.DistinguishedName = settings.Server.ConfigDn;
    accessModify.Modifications.Add(accessAttr);
    var modifyRsp = await ldap.SendRequestAsync(accessModify);
    if (modifyRsp.ResultCode != 0) throw new PavedMessageException($"failed to modify: {modifyRsp.ErrorMessage}");

    WriteLine("Completed.");
});
