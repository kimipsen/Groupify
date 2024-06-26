using System.Linq;

using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;

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
    [Parameter("Artifacts Type")] readonly string ArtifactsType;
    [Parameter("Excluded Artifacts Type")] readonly string ExcludedArtifactsType;
    [Parameter("Nuget Feed Url for Public Access of Releases")] readonly string NugetFeed;
    [Parameter("Nuget Api Key"), Secret] readonly string NugetApiKey;
    [Parameter("MyGet Feed Url for Public Access of Pre Releases")] readonly string MyGetNugetFeed;
    [Parameter("MyGet Api Key"), Secret] readonly string MyGetApiKey;
    [Parameter("Nuget package description")] readonly string NugetDescription;
    [Parameter("Nuget package copyright")] readonly string Copyright;

    [GitRepository] readonly GitRepository Repository;
    [Solution(GenerateProjects = true)] readonly Solution Solution;
    AbsolutePath PackagesDirectory => RootDirectory / "packages";

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
            DotNetTasks.DotNetRestore(s => s);
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // compile actual code
            DotNetTasks.DotNetBuild(b => b
                .SetConfiguration(Configuration)
                .SetVersion(GitVersion.NuGetVersionV2)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .EnableNoRestore()
            );
        });

    Target MutationTests => _ => _
        .DependsOn(Compile)
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

    Target Pack => _ => _
        .Triggers(PushGithub, PushNugetOrg, PushMyget)
        .DependsOn(Compile, MutationTests, Test)
        .Executes(() =>
        {
            // push nuget package to github
            DotNetTasks.DotNetPack(s => s
                .SetProject(Solution.Groupify_Core)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackagesDirectory)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetVersion(GitVersion.NuGetVersionV2)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetPackageId("Groupify.Core")
                .SetAuthors(GitHubUser)
                .SetDescription(NugetDescription)
                .SetCopyright(Copyright)
            );
        });

    Target PushNugetOrg => _ => _
        .Description($"Publishing to NuGet with the version.")
        .Triggers(Release)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .OnlyWhenStatic(() => Repository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            PackagesDirectory.GlobFiles(ArtifactsType)
            .Where(x => !x.Name.EndsWith(ExcludedArtifactsType))
            .ForEach(x =>
            {
                DotNetTasks.DotNetNuGetPush(s => s
                    .SetTargetPath(x)
                    .SetSource(NugetFeed)
                    .SetApiKey(NugetApiKey)
                    .EnableSkipDuplicate()
                );
            });
        });

    Target PushMyget => _ => _
        .Description($"Publishing to MyGet for PreRelese only.")
        .Triggers(Release)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .OnlyWhenStatic(() => Repository.IsOnReleaseBranch())
        .Executes(() =>
        {
            PackagesDirectory.GlobFiles(ArtifactsType)
            .Where(x => !x.Name.EndsWith(ExcludedArtifactsType))
            .ForEach(x =>
            {
                DotNetTasks.DotNetNuGetPush(s => s
                    .SetTargetPath(x)
                    .SetSource(MyGetNugetFeed)
                    .SetApiKey(MyGetApiKey)
                    .EnableSkipDuplicate()
                );
            });
        });
}
