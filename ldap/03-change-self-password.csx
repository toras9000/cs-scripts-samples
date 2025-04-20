#r "nuget: System.DirectoryServices, 9.0.4"
#r "nuget: System.DirectoryServices.Protocols, 9.0.4"
#r "nuget: Lestaly, 0.74.0"
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

    // LDAP directory info
    Directory = new
    {
        // DN to manage users
        UsersDn = "ou=persons,ou=accounts,dc=myserver,o=home",

        // RDN attribute
        RdnAttr = "uid",
    },
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Preparation for LDAP server connection
    WriteLine("Preparation for LDAP server connection");
    var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.AutoBind = true;

    while (true)
    {
        // The stage in which the change target is entered.
        WriteLine();
        WriteLine(Chalk.Inverse[$"Designation of password change target"]);

        // Enter authentication information.
        var username = ConsoleWig.WriteLine("Username").Write(">").ReadLine();
        if (username.IsWhite()) break;

        var password = ConsoleWig.WriteLine("Password (no echo back)").Write(">").ReadLineIntercepted();
        WriteLine();

        // User DN
        var userDn = $"{settings.Directory.RdnAttr}={username},{settings.Directory.UsersDn}";

        try
        {
            // Set authentication information
            ldap.Credential = new NetworkCredential(userDn, password);

            // Perform a search request.
            _ = await ldap.GetEntryAsync(userDn) ?? throw new PavedMessageException("not found");

            WriteLine(Chalk.Green[$"Successful user binding"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
            continue;
        }

        // The stage of changing passwords.
        WriteLine();
        WriteLine(Chalk.Inverse[$"Enter change information"]);

        try
        {
            // It should be possible to retry several times in case of input errors.
            var newpass = default(string);
            for (var i = 0; i < 3; i++)
            {
                // Enter new password
                var input1 = ConsoleWig.WriteLine("New Password (no echo back)").Write(">").ReadLineIntercepted();
                WriteLine();
                var input2 = ConsoleWig.WriteLine("Re-enter for confirmation (no echo back)").Write(">").ReadLineIntercepted();
                WriteLine();

                // Verification of input results
                if (input1 != input2) { WriteLine("Input does not match. Please try again."); continue; }
                if (input1.IsWhite()) { WriteLine("Empty passwords are not acceptable."); continue; }

                // If there is no problem, it will be confirmed.
                newpass = input1;
                break;
            }

            // Verify that there were no input problems.
            if (newpass == null) throw new PavedMessageException("The change was discontinued because of repeated failures.");

            // Hash the password.
            var encPass = LdapExtensions.MakePasswordHash.SHA256(newpass);

            // Create password attribute change information.
            var changePass = new DirectoryAttributeModification();
            changePass.Operation = DirectoryAttributeOperation.Replace;
            changePass.Name = "userPassword";
            changePass.Add(encPass);

            // Request changes.
            var updateReq = new ModifyRequest();
            updateReq.DistinguishedName = userDn;
            updateReq.Modifications.Add(changePass);
            var updateRsp = await ldap.SendRequestAsync(updateReq);
            if (updateRsp.ResultCode != 0) throw new PavedMessageException($"failed to request: code={updateRsp.ResultCode}, msg={updateRsp.ErrorMessage}");

            WriteLine(Chalk.Green[$"Successfully changed password"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
            continue;
        }
    }

});
