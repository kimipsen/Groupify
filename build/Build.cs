using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;

namespace Groupify.Build;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    InvokedTargets = [nameof(Prep)])]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    GitHubActions GitHubActions => GitHubActions.Instance;

    [Solution(GenerateProjects = true)] readonly Solution Solution;

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
        });

    Target Prep => _ => _
        .DependsOn(Compile, Test)
        .Executes(() =>
        {
        });

    Target Release => _ => _
        .DependsOn(Codeanalysis, MutationTests, Prep)
        .Executes(() =>
        {
            // do some github magic to release project
        });

    Target MutationTests => _ => _
        .DependsOn(Compile)
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
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // run dotnet tests
            DotNetTasks.DotNetTest();
        });
}
