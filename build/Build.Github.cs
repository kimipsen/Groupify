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
    FetchDepth = 0)]
public partial class Build
{
    GitHubActions GitHubActions => GitHubActions.Instance;
    [OctoVersion(AutoDetectBranch = true)] readonly OctoVersionInfo OctoVersionInfo;

    Target Release => _ => _
        .Requires(() => Repository.IsOnMainOrMasterBranch())
        .DependsOn(Test, MutationTests)
        .Executes(() =>
        {
            var credentials = new Credentials(GitHubActions.Token);
            GitHubTasks.GitHubClient = new GitHubClient(
                new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));
        });
}
