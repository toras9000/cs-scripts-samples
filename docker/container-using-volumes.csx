#!/usr/bin/env -S dotnet-script
#r "nuget: Docker.DotNet, 3.125.15"
#r "nuget: Lestaly.General, 0.112.0"
#nullable enable
using Docker.DotNet;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    using var client = new DockerClientConfiguration().CreateClient();
    var containerList = await client.Containers.ListContainersAsync(new() { All = true, }, signal.Token);
    foreach (var container in containerList)
    {
        WriteLine($"{container.Names.Select(n => n.TrimStart('/')).JoinString(", ")}");
        var volumeMounts = container.Mounts.Where(m => m.Type == "volume").ToArray();
        if (volumeMounts.Length <= 0)
        {
            WriteLine($"    no volume mounted");
            continue;
        }
        foreach (var mount in volumeMounts)
        {
            WriteLine($"    {mount.Name} - {mount.Source}");
        }
    }
});
