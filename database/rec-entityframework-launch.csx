#r "nuget: Lestaly, 0.82.0"
using Lestaly;
using Lestaly.Cx;

await "dotnet".args("script", "--isolated-load-context", ThisSource.RelativeFile("rec-entityframework.csx"));
