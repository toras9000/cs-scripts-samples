#r "nuget: AngleSharp, 1.2.0"
#r "nuget: Lestaly, 0.73.0"
#nullable enable
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AngleSharp;
using AngleSharp.Html.Dom;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // send csr file
    UseCsrFile = ThisSource.RelativeFile("../certs/client.csr"),

    // certificate request site
    RequestSite = "https://test.mosquitto.org/ssl/",

    // save cert file
    SaveClientCert = ThisSource.RelativeFile("../certs/client.crt"),
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    WriteLine("Read csr file");
    var csrPem = await settings.UseCsrFile.ReadAllTextAsync(signal.Token);

    WriteLine("Access request site");
    var config = Configuration.Default.WithDefaultLoader();
    using var context = BrowsingContext.New(config);
    using var document = await context.OpenAsync(settings.RequestSite, signal.Token);

    WriteLine("Request certificate generation");
    var reqForm = document.Forms.First();
    var rspDoc = await reqForm.SubmitAsync(new { csr = csrPem, });
    var rspCrt = rspDoc.Body!.TextContent;

    WriteLine("Verify certificate");
    X509Certificate2.CreateFromPem(rspCrt);

    WriteLine("Save certificate file");
    await settings.SaveClientCert.WriteAllTextAsync(rspCrt);
});
