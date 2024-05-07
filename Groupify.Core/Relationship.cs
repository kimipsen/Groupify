namespace Groupify.Core;
public record class Relationship(IPerson Person1, IPerson Person2, RelationshipType RelationshipType);
