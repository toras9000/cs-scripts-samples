#r "nuget: Lestaly, 0.84.0"
#nullable enable
using System.Net.Http;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // CA cert url
    CaCertUrl = new Uri("http://test.mosquitto.org/ssl/mosquitto.org.crt"),

    // save CA cert file
    SaveCaCert = ThisSource.RelativeFile("../certs/ca.crt"),
};

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Download CA cert file");
    using var http = new HttpClient();
    await http.GetFileAsync(settings.CaCertUrl, settings.SaveCaCert.FullName, signal.Token);
});
