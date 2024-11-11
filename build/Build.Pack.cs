using System.IO.Enumeration;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target Pack => definition => definition
        .DependsOn(Clean)
        //.OnlyWhenStatic(() => IsLocalBuild || GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            ValidateRelease();

            DotNetPack(settings => settings
                .SetConfiguration("Abstractions Release")
                .SetProject(Solution.SchemaMigrations_Abstractions)
                .SetVersion(AbstractionVersion)
                .SetOutputDirectory($"{ArtifactsDirectory}/{Solution.SchemaMigrations_Abstractions.Name}")
                .SetVerbosity(DotNetVerbosity.minimal)
                .SetPackageReleaseNotes(CreateNugetChangelog()));

            DotNetNuGetAddSource(settings => settings
                .SetName("TempSource")
                .SetSource(ArtifactsDirectory));

            foreach (var configuration in GlobBuildConfigurations())
                DotNetPack(settings => settings
                    .SetConfiguration(configuration)
                    .SetProject(Solution.SchemaMigrations_Database)
                    .SetVersion(GetPackVersion(configuration))
                    .SetProperty("PackEnabled", true)
                    .SetOutputDirectory($"{ArtifactsDirectory}/{Solution.SchemaMigrations_Database.Name}")
                    .SetVerbosity(DotNetVerbosity.minimal)
                    .SetPackageReleaseNotes(CreateNugetChangelog()));

            DotNetPack(settings => settings
                .SetConfiguration("Generator Release")
                .SetProject(Solution.SchemaMigrations_Generator)
                .SetVersion(GeneratorVersion)
                .SetOutputDirectory($"{ArtifactsDirectory}/{Solution.SchemaMigrations_Generator.Name}")
                .SetVerbosity(DotNetVerbosity.minimal));

            using var process = ProcessTasks.StartProcess(DotNetPath, arguments: "dotnet nuget remove source TempSource");
        });

    string GetPackVersion(string configuration)
    {
        if (VersionMap.TryGetValue(configuration, out var version)) return version;
        throw new Exception($"Can't find pack version for configuration: {configuration}");
    }

    string CreateNugetChangelog()
    {
        Assert.True(File.Exists(ChangeLogPath), $"Unable to locate the changelog file: {ChangeLogPath}");
        Log.Information("Changelog: {Path}", ChangeLogPath);

        var changelog = BuildChangelog();
        Assert.True(changelog.Length > 0, $"No version entry exists in the changelog: {Version}");

        return EscapeMsBuild(changelog.ToString());
    }

    static string EscapeMsBuild(string value)
    {
        return value
            .Replace(";", "%3B")
            .Replace(",", "%2C");
    }

    List<string> GlobBuildConfigurations()
    {
        var configurations = Solution.Configurations
            .Where(pair => !pair.Key.Contains("Generator"))
            .Where(pair => !pair.Key.Contains("Abstractions"))
            .Select(pair => pair.Key)
            .Select(config => config.Remove(config.LastIndexOf('|')))
            .Where(config => Configurations.Any(wildcard => FileSystemName.MatchesSimpleExpression(wildcard, config)))
            .ToList();

        Assert.NotEmpty(configurations, $"No solution configurations have been found. Pattern: {string.Join(" | ", Configurations)}");
        return configurations;
    }
}