namespace Groupify.Core;

public class RelationshipType(string name, int value) : IRelationshipType
{
    public static readonly RelationshipType Match = new("Match", -1);
    public static readonly RelationshipType DoNotMatch = new("Do not match", 1000);

    public string Name => name;
    public int Value => value;
}
