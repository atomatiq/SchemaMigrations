sealed partial class Build
{
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
            { "Release R21", "2021.0.2" },
            { "Release R22", "2022.0.2" },
            { "Release R23", "2023.0.2" },
            { "Release R24", "2024.0.2" },
            { "Release R25", "2025.0.2" }
        };
        
        GeneratorVersion = "1.0.1";
        AbstractionVersion = "1.0.1";
    }
}