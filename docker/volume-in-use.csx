#!/usr/bin/env -S dotnet-script
#r "nuget: Docker.DotNet, 3.125.15"
#r "nuget: Lestaly.General, 0.109.0"
#nullable enable
using Docker.DotNet;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();

    using var client = new DockerClientConfiguration().CreateClient();
    var volumeList = await client.Volumes.ListAsync(signal.Token);
    var containerList = await client.Containers.ListContainersAsync(new() { All = true, }, signal.Token);

    var mountContainers = containerList
        .SelectMany(c => c.Mounts.Select(m => new { Container = c, Mount = m, }))
        .Where(m => m.Mount.Type == "volume")
        .ToLookup(m => m.Mount.Source);

    foreach (var vol in volumeList.Volumes)
    {
        WriteLine($"{vol.Name}");
        var mounts = mountContainers[vol.Mountpoint].ToArray();
        if (mounts.Length <= 0)
        {
            WriteLine($"    not in use");
            continue;
        }
        foreach (var mount in mounts)
        {
            var names = mount.Container.Names.Select(n => n.TrimStart('/')).JoinString(", ");
            var id = mount.Container.ID[..8];
            var image = mount.Container.Image;
            WriteLine($"    {names} ({id}) [{image}]");
        }
    }
});
