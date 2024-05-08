using System;

using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;

namespace Groupify.Build;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository Repository;
    [Solution(GenerateProjects = true)] readonly Solution Solution;

    AbsolutePath PackagesDirectory => RootDirectory / "packages";

    [Parameter, Secret] readonly string NugetApiKey;

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetClean();
            PackagesDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            // do some restores on nuget etc
            DotNetTasks.DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // compile actual code
            DotNetTasks.DotNetBuild();
        });

    Target MutationTests => _ => _
        .DependsOn(Compile, Test)
        .Executes(() =>
        {
            DotNetTasks.DotNetToolRestore();
            // run stryker tool
            DotNetTasks.DotNet("stryker");
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // run dotnet tests
            DotNetTasks.DotNetTest();
        });

    Target AddNugetSource => _ => _
        .Requires(() => GitHubUser)
        .Requires(() => GitHubToken)
        .Executes(() =>
        {
            try
            {
                NuGetTasks.NuGetSourcesAdd(s => s
                    .SetName("nuget.org")
                    .SetSource($"https://nuget.pkg.github.com/{GitHubUser}/index.json")
                );
            }
            catch
            {
                Console.WriteLine("Source (nuget.org) already exists");
            }
        });

    Target Pack => _ => _
    .DependsOn(AddGithubSource, AddNugetSource)
    .Executes(() =>
    {
        // push nuget package to github
        DotNetTasks.DotNetPack(s => s
            .SetProject(RootDirectory / "Groupify.Core")
            .SetOutputDirectory(PackagesDirectory)
            .SetVersion(OctoVersionInfo.FullSemVer)
            .SetPackageId("Groupify.Core")
            .SetAuthors(GitHubUser)
            .SetDescription("")
            .SetConfiguration(Configuration)
        );
    });

    Target PushNugetOrg => _ => _
        .DependsOn(Pack, Release)
        .Executes(() =>
        {
            DotNetTasks.DotNetNuGetPush(s => s
                .SetTargetPath(PackagesDirectory / "*.nupkg")
                .SetApiKey(NugetApiKey)
                .SetSource("nuget.org")
            );
        });
}
