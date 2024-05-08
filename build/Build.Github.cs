using System;

using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.OctoVersion;

using Octokit;
using Octokit.Internal;

namespace Groupify.Build;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    InvokedTargets = [nameof(PushNugetOrg)],
    EnableGitHubToken = true,
    FetchDepth = 0,
    ImportSecrets = [nameof(NugetApiKey)])]
public partial class Build
{
    GitHubActions GitHubActions => GitHubActions.Instance;
    [OctoVersion(AutoDetectBranch = true)] readonly OctoVersionInfo OctoVersionInfo;
    [Nuke.Common.Parameter] readonly string GitHubUser = GitHubActions.Instance?.RepositoryOwner;
    [Nuke.Common.Parameter, Secret] readonly string GitHubToken;


    Target Release => _ => _
        .Requires(() => Repository.IsOnMainOrMasterBranch())
        .DependsOn(Test, MutationTests)
        .Executes(() =>
        {
            var credentials = new Credentials(GitHubActions.Token);
            GitHubTasks.GitHubClient = new GitHubClient(
                new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));

            NewRelease release = new(OctoVersionInfo.FullSemVer);

            GitHubTasks.GitHubClient.Repository.Release.Create(GitHubUser, GitHubToken, release);
        });

    // https://blog.raulnq.com/github-packages-publishing-nuget-packages-using-nuke-with-gitversion-and-github-actions#heading-create-a-github-action-workflow
    // Target AddGithubSource => _ => _
    //     .Requires(() => GitHubUser)
    //     .Requires(() => GitHubToken)
    //     .Executes(() =>
    //     {
    //         try
    //         {
    //             DotNetTasks.DotNetNuGetAddSource(s => s
    //                 .SetName("github")
    //                 .SetUsername(GitHubUser)
    //                 .SetPassword(GitHubToken)
    //                 .EnableStorePasswordInClearText()
    //                 .SetSource($"https://nuget.pkg.github.com/{GitHubUser}/index.json")
    //             );
    //         }
    //         catch
    //         {
    //             Console.WriteLine("Source (github) already exists");
    //         }
    //     });

    // Target PushGithub => _ => _
    //     .DependsOn(Pack, AddGithubSource)
    //     .Executes(() =>
    //     {
    //         DotNetTasks.DotNetNuGetPush(s => s
    //             .SetTargetPath(PackagesDirectory / "*.nupkg")
    //             .SetApiKey(GitHubToken)
    //             .SetSource("github")
    //         );
    //     });
}
