using System;

using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.OctoVersion;

using Octokit;
using Octokit.Internal;

namespace Groupify.Build;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    InvokedTargets = [nameof(Release)],
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
        // .DependsOn(Test, MutationTests)
        .Executes(async () =>
        {
            var credentials = new Credentials(GitHubActions.Token);
            GitHubTasks.GitHubClient = new GitHubClient(
                new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));

            NewTag newTag = new()
            {
                Tag = OctoVersionInfo.FullSemVer,
                Object = Repository.Commit,
                Type = TaggedType.Commit,
                Tagger = new Committer("Kim Ipsen", "kim.ipsen@outlook.dk", DateTimeOffset.UtcNow)
            };
            // await GitHubTasks.GitHubClient.Git.Tag.Create("Kim Ipsen", OctoVersionInfo.FullSemVer, newTag);

            // NewRelease newRelease = new(OctoVersionInfo.FullSemVer);

            // await GitHubTasks.GitHubClient.Repository.Release.Create(GitHubUser, GitHubToken, newRelease);
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
