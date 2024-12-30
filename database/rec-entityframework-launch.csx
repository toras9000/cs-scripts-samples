#r "nuget: ProcessX, 1.5.5"
#r "nuget: Lestaly, 0.69.0"
using Zx;
using Lestaly;

var script = ThisSource.RelativeFile("rec-entityframework.csx");
await $"dotnet script --isolated-load-context {script.FullName}";
