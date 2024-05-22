using System;

using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
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
    [GitVersion]readonly GitVersion GitVersion;
    [Nuke.Common.Parameter] readonly string GitHubUser = GitHubActions.Instance?.RepositoryOwner;
    [Nuke.Common.Parameter, Secret] readonly string GitHubToken;

    Target Release => _ => _
        .Requires(() => Repository.IsOnMainOrMasterBranch())
        // .DependsOn(Test, MutationTests)
        .Executes(async () =>
        {
            //https://anktsrkr.github.io/post/manage-your-package-release-using-nuke-in-github/
            var credentials = new Credentials(GitHubActions.Token);
            var (owner, name) = (Repository.GetGitHubOwner(), Repository.GetGitHubName());

            GitHubTasks.GitHubClient = new GitHubClient(
                new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));

            string releaseTag = GitVersion.NuGetVersionV2;
            NewRelease newRelease = new(releaseTag)
            {
                TargetCommitish = GitVersion.Sha,
                Draft = true,
                Name = $"v{releaseTag}",
                Prerelease = !string.IsNullOrWhiteSpace(GitVersion.PreReleaseTag),
                Body = "",
            };

            Release createdRelease = await GitHubTasks
                .GitHubClient
                .Repository
                .Release
                .Create(owner, name, newRelease)
            ;

            // TODO: upload files

            await GitHubTasks
                .GitHubClient
                .Repository
                .Release
                .Edit(owner, name, createdRelease.Id, new ReleaseUpdate{ Draft = false });
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
