using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;

using Octokit;
using Octokit.Internal;
using System.Linq;

namespace Groupify.Build;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    InvokedTargets = [nameof(Pack)],
    EnableGitHubToken = true,
    FetchDepth = 0,
    ImportSecrets = [nameof(NugetApiKey)],
    OnPushBranches = ["main", "master", "dev", "releases/**"],
    OnPullRequestBranches = ["releases/**"])]
public partial class Build
{
    GitHubActions GitHubActions => GitHubActions.Instance;
    [GitVersion]readonly GitVersion GitVersion;
    [Nuke.Common.Parameter] readonly string GitHubUser = GitHubActions.Instance?.RepositoryOwner;

    string GithubNugetFeed => GitHubActions != null 
         ? $"https://nuget.pkg.github.com/{GitHubActions.RepositoryOwner}/index.json"
         : null;

    Target Release => _ => _
        .Requires(() => Repository.IsOnMainOrMasterBranch())
        // .DependsOn(Test, MutationTests)
        .OnlyWhenStatic(() => Repository.IsOnMainOrMasterBranch() || Repository.IsOnDevelopBranch())
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

    Target PushGithub => _ => _
        .Triggers(Release)
        .Description($"Publishing to Github for Development only.")
        .OnlyWhenStatic(() => Repository.IsOnDevelopBranch() || GitHubActions.IsPullRequest)
        .Executes(() =>
        {
            PackagesDirectory.GlobFiles(ArtifactsType)
            .Where(x => !x.Name.EndsWith(ExcludedArtifactsType))
            .ForEach(x => {
                DotNetTasks.DotNetNuGetPush(s => s
                    .SetTargetPath(x)
                    .SetSource(GithubNugetFeed)
                    .SetApiKey(GitHubActions.Token)
                    .EnableSkipDuplicate()
                );
            });
        });
}
