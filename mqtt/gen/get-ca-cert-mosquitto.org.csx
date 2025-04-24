#r "nuget: Lestaly, 0.75.0"
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

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    using var signal = ConsoleWig.CreateCancelKeyHandlePeriod();

    WriteLine("Download CA cert file");
    using var http = new HttpClient();
    await http.GetFileAsync(settings.CaCertUrl, settings.SaveCaCert.FullName, signal.Token);
});
