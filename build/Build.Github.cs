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
    EnableGitHubToken = true)]
public partial class Build
{
    GitHubActions GitHubActions => GitHubActions.Instance;
    [OctoVersion] readonly OctoVersionInfo OctoVersionInfo;

    Target Release => _ => _
        .Requires(() => Repository.IsOnMainOrMasterBranch())
        .DependsOn(Codeanalysis, MutationTests)
        .Executes(() =>
        {
            var credentials = new Credentials(GitHubActions.Token);
            GitHubTasks.GitHubClient = new GitHubClient(
                new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));
        });
}
