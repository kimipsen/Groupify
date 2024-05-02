namespace Groupify.Core.Test;

public class GenerateTest
{
    [Fact]
    public void GenerateGroups()
    {
        GroupSettings settings = new()
        {
            GroupSize = 3,
            NumberOfGroups = 3,
            UseGroupSize = true,
        };
        List<Person> people = [
            new(1, "A", "B"), new(2, "C", "D"), new(3, "E", "F"),
            new(4, "G", "H"), new(5, "I", "J"), new(6, "K", "L"),
            new(7, "M", "N"), new(8, "O", "P"), new(9, "Q", "R"),
            new(10, "S", "T")
        ];
        List<Relationship> relations = [
        ];
        var sut = new Generator(settings, people, relations);

        sut.GenerateGroups();
    }
}