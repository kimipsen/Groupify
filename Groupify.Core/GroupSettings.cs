namespace Groupify.Core;

public class GroupSettings
{
    public bool UseGroupSize { get; set; } = false;
    public int NumberOfGroups { get; set; } = 1;
    public int GroupSize { get; set; } = 1;

    public bool ValidateSettings()
    {
        if (UseGroupSize && GroupSize < 1) return false;
        if (!UseGroupSize && NumberOfGroups < 1) return false;
        return true;
    }
}