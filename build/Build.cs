using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;

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

    [Secret, Parameter] readonly string SonarCloudtoken;

    Target Restore => _ => _
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

    Target Codeanalysis => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // run sonarcloud
            SonarScannerTasks.SonarScannerBegin();
            SonarScannerTasks.SonarScannerEnd();
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // run dotnet tests
            DotNetTasks.DotNetTest();
        });
}
