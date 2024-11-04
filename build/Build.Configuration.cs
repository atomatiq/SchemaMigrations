sealed partial class Build
{
    const string GeneratorVersion = "1.0.0";
    const string AbstractionVersion = "1.0.0";
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "output";
    readonly AbsolutePath ChangeLogPath = RootDirectory / "Changelog.md";
    string PublishVersion => Version ??= VersionMap.Values.Last();

    protected override void OnBuildInitialized()
    {
        Configurations =
        [
            "Release*"
        ];

        VersionMap = new()
        {
            { "Release R21", "2021.1.0" },
            { "Release R22", "2022.1.0" },
            { "Release R23", "2023.1.0" },
            { "Release R24", "2024.1.0" },
            { "Release R25", "2025.1.0" }
        };
    }
}