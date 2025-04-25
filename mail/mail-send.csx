#r "nuget: MailKit, 4.11.0"
#nullable enable
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using MailKit.Security;

{
    var mailTo = new MailboxAddress("user1", "user1@myserver.home");
    var mailFrom = new MailboxAddress("user2", "user2@myserver.home");
    var mail = new MimeMessage();
    mail.From.Add(mailTo);
    mail.To.Add(mailFrom);
    mail.Subject = "How you doin'?";
    mail.Body = new TextPart("plain", """
    This is Test message.
    Using MailKit
    """);

    using var client = new SmtpClient();
    await client.ConnectAsync("myserver.home", 25, SecureSocketOptions.None);
    await client.SendAsync(mail);
    await client.DisconnectAsync(quit: true);
}
