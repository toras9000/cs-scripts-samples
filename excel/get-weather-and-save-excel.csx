#r "nuget: Lestaly.General, 0.105.0"
#r "nuget: Lestaly.Excel, 0.100.0"
#nullable enable
using System.Net.Http;
using System.Net.Http.Json;
using Lestaly;

var settings = new
{
    Position = new
    {
        Latitude = 35.652832,
        Longitude = 139.839478,
    },
};

record OpenMetroUnits(
    string time,
    string temperature_2m,
    string relative_humidity_2m,
    string pressure_msl,
    string cloud_cover,
    string precipitation,
    string precipitation_probability
);

record OpenMetroData(
    DateTime[] time,
    double[] temperature_2m,
    double[] relative_humidity_2m,
    double[] pressure_msl,
    double[] cloud_cover,
    double[] precipitation,
    double[] precipitation_probability
);

record OpenMetro(
    double latitude,
    double longitude,
    string timezone,
    OpenMetroUnits hourly_units,
    OpenMetroData hourly
);

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine($"Obtain weather information ...");
    var weatherParams = typeof(OpenMetroData).GetProperties().Select(p => p.Name).Where(n => n != nameof(OpenMetroData.time));
    var openMetroEp = new Uri($"https://api.open-meteo.com/v1/forecast?latitude={settings.Position.Latitude}&longitude={settings.Position.Longitude}&hourly={weatherParams.JoinString(",")}");

    using var client = new HttpClient();
    var weather = await client.GetFromJsonAsync<OpenMetro>(openMetroEp, signal.Token);
    if (weather == null) throw new Exception("Cannnot get weather");

    var saveFile = ThisSource.RelativeFile($"open-metro-{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    WriteLine($"Save to an Excel file ... {saveFile.Name}");
    var saveOptions = new SaveToExcelOptions();
    saveOptions.CaptionSelector = (m, i) => m.Name switch
    {
        nameof(OpenMetroRow.Temperature) => $"Temperature[{weather.hourly_units.temperature_2m}]",
        nameof(OpenMetroRow.Humidity) => $"Humidity[{weather.hourly_units.relative_humidity_2m}]",
        nameof(OpenMetroRow.Pressure) => $"Pressure[{weather.hourly_units.pressure_msl}]",
        nameof(OpenMetroRow.CloudCover) => $"CloudCover[{weather.hourly_units.cloud_cover}]",
        nameof(OpenMetroRow.Precipitation) => $"Precipitation[{weather.hourly_units.precipitation}]",
        nameof(OpenMetroRow.PrecipitationProbability) => $"Precipitation[{weather.hourly_units.precipitation_probability}]",
        _ => null
    };
    weather.hourly.time
        .Select((t, i) => new OpenMetroRow(
            t,
            weather.hourly.temperature_2m[i],
            weather.hourly.relative_humidity_2m[i],
            weather.hourly.pressure_msl[i],
            weather.hourly.cloud_cover[i],
            weather.hourly.precipitation[i],
            weather.hourly.precipitation_probability[i]
        ))
        .SaveToExcel(saveFile, saveOptions);

    WriteLine($"Completed");
});

record OpenMetroRow(
    DateTime Time,
    double Temperature,
    double Humidity,
    double Pressure,
    double CloudCover,
    double Precipitation,
    double PrecipitationProbability
);
