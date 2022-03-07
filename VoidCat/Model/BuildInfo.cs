using System.Reflection;

namespace VoidCat.Model;

public class BuildInfo
{
    public string? Version { get; init; }
    public string? GitHash { get; init; }
    public DateTime BuildTime { get; init; }

    public static BuildInfo GetBuildInfo()
    {
        var asm = Assembly.GetEntryAssembly();
        var version = asm.GetName().Version;

        var gitHash = asm
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => attr.Key == "GitHash")?.Value;

        var buildTime = asm
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => attr.Key == "BuildTime");

        return new()
        {
            Version = $"{version.Major}.{version.Minor}.{version.Build}",
            GitHash = gitHash,
            BuildTime = DateTime.FromBinary(long.Parse(buildTime?.Value ?? "0"))
        };
    }
}