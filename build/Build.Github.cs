using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.GitVersion;

namespace Groupify.Build;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = [GitHubActionsTrigger.Push],
    InvokedTargets = [nameof(Release)])]
public partial class Build
{
    GitHubActions GitHubActions => GitHubActions.Instance;
    [GitVersion] readonly GitVersion GitVersion;
}
