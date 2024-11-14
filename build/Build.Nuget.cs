using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using RevitExtensions.Build.Tools;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static RevitExtensions.Build.Tools.DotNetExtendedTasks;

partial class Build
{
    const string NugetApiUrl = "https://api.nuget.org/v3/index.json";

    Target NuGetPush => definition => definition
        .DependsOn(Pack)
        .Requires(() => NugetApiKey)
        .OnlyWhenStatic(() => IsLocalBuild || GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            var abstractionsDirectory = AbsolutePath.Create($"{ArtifactsDirectory}/{Solution.SchemaMigrations_Abstractions.Name}");
            var databaseDirectory = AbsolutePath.Create($"{ArtifactsDirectory}/{Solution.SchemaMigrations_Database.Name}");
            var generatorDirectory = AbsolutePath.Create($"{ArtifactsDirectory}/{Solution.SchemaMigrations_Generator.Name}");

            if (All || Abstractions)
            {
                foreach (var package in abstractionsDirectory.GlobFiles("*.nupkg"))
                    DotNetNuGetPush(settings => settings
                        .SetTargetPath(package)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey));
            }

            if (All || Database)
            {
                foreach (var package in databaseDirectory.GlobFiles("*.nupkg"))
                    DotNetNuGetPush(settings => settings
                        .SetTargetPath(package)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey));
            }

            if (All || Generator)
            {
                foreach (var package in generatorDirectory.GlobFiles("*.nupkg"))
                    DotNetNuGetPush(settings => settings
                        .SetTargetPath(package)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey));
            }
        });

    Target NuGetDelete => definition => definition
        .Requires(() => NugetApiKey)
        .OnlyWhenStatic(() => IsLocalBuild && GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            foreach (var (_, version) in VersionMap)
            {
                DotNetNuGetDelete(settings => settings
                    .SetPackage("Atomatiq.SchemaMigrations.Database")
                    .SetVersion(version)
                    .SetSource(NugetApiUrl)
                    .SetApiKey(NugetApiKey)
                    .EnableNonInteractive());
            }

            foreach (var (_, version) in VersionMap)
            {
                DotNetNuGetDelete(settings => settings
                    .SetPackage("Atomatiq.SchemaMigrations.Abstractions")
                    .SetVersion(version)
                    .SetSource(NugetApiUrl)
                    .SetApiKey(NugetApiKey)
                    .EnableNonInteractive());
            }

            DotNetNuGetDelete(settings => settings
                .SetPackage("Atomatiq.SchemaMigrations.Generator")
                .SetVersion(GeneratorVersion)
                .SetSource(NugetApiUrl)
                .SetApiKey(NugetApiKey)
                .EnableNonInteractive());
        });
}