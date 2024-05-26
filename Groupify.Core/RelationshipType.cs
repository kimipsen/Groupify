namespace Groupify.Core;

/// <summary>
/// Default implmentation of IRelationshipType, to showcase how they can be used to mark combinations of people as `match` and `do not match`.
/// </summary>
/// <param name="name">A name, currently not used for anything other than distinguishing different relationship types</param>
/// <param name="value">A numerical value, used in the score calculation, when testing how good a group match is</param>
public class RelationshipType(string name, int value) : IRelationshipType
{
    /// <summary>
    /// Allows matching 2 people. Lower values are better.
    /// </summary>
    public static readonly RelationshipType Match = new("Match", -1);
    /// <summary>
    /// Allows marking a bad relationship between people. Higher values are worse.
    /// </summary>
    public static readonly RelationshipType DoNotMatch = new("Do not match", 1000);

    public string Name => name;
    public int Value => value;
}
