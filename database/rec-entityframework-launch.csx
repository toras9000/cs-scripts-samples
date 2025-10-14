#r "nuget: Lestaly.General, 0.106.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

await "dotnet".args("script", "--isolated-load-context", ThisSource.RelativeFile("rec-entityframework.csx"));
