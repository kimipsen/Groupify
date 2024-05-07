namespace Groupify.Core;

public record class Group(int Id, int MaxSize)
{
    public List<IPerson> People { get; init; } = [];
    public bool IsFilled => People.Count == MaxSize;

    public Group Copy()
    {
        return new(Id, MaxSize)
        {
            People = new(People),
        };
    }
}
