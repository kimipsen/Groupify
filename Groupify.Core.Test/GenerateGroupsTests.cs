using FluentAssertions;

namespace Groupify.Core.Test;

public class GenerateGroupsTests
{
    [Fact]
    public void GenerateGroups_WhenOptimizing_ShouldNotThrowExceptions()
    {
        GroupSettings settings = new()
        {
            GroupSize = 3,
            UseGroupSize = true,
        };
        List<IPerson> people = Fakers.People.Generate(10);
        List<Relationship> relations = [
            // group 1+2+3
            new(people[0], people[1], RelationshipType.Match),
            new(people[0], people[2], RelationshipType.Match),
            new(people[1], people[2], RelationshipType.Match),
            // ensure 5+6 are NOT grouped with 7+8
            new(people[4], people[6], RelationshipType.DoNotMatch),
            new(people[5], people[6], RelationshipType.DoNotMatch),
            new(people[4], people[7], RelationshipType.DoNotMatch),
            new(people[5], people[7], RelationshipType.DoNotMatch)
        ];
        var sut = new Generator(settings, people, relations);

        Action action = () => sut.GenerateGroups();

        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateGroups_WhengivenNegativeValueForGroupSize_ShouldStop()
    {
        GroupSettings settings = new()
        {
            NumberOfGroups = -1,
            UseGroupSize = false,
        };

        List<IPerson> people = Fakers.People.Generate(1);
        List<Relationship> relationships = [];
        Generator systemUnderTest = new(settings, people, relationships);
        systemUnderTest.GenerateGroups();

        systemUnderTest.Groups.Should().BeEmpty();
    }

    [Theory]
    //[InlineData(2)]
    [InlineData(50)]
    [InlineData(100)]
    public void GenerateGroups_WhenSettingNumberOfGroups_ShouldAlwaysReturnTheSameNumberOfGroups(int groupCount)
    {
        GroupSettings settings = new()
        {
            NumberOfGroups = groupCount,
            UseGroupSize = false,
        };

        List<IPerson> people = Fakers.People.Generate(250);
        List<Relationship> relationships = [];
        Generator systemUnderTest = new(settings, people, relationships);
        systemUnderTest.GenerateGroups();

        systemUnderTest.Groups.Should().HaveCount(groupCount);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(25)]
    [InlineData(50)]
    public void GenerateGroups_WhenSettingNumberOfGroups_ShouldAlwaysBalanceAmountOfPeopleInGroups(int groupCount)
    {
        GroupSettings settings = new()
        {
            NumberOfGroups = groupCount,
            UseGroupSize = false,
        };

        List<IPerson> people = Fakers.People.Generate(groupCount * 4);
        List<Relationship> relationships = [];
        Generator systemUnderTest = new(settings, people, relationships);
        systemUnderTest.GenerateGroups();

        systemUnderTest.Groups.Should().AllSatisfy(g => g.People.Should().HaveCount(4));
    }

    [Fact]
    public void GenerateGroups_WhenPeopleDoesNotWantToBeMatched()
    {
        GroupSettings settings = new()
        {
            GroupSize = 2,
            UseGroupSize = true,
        };
        List<IPerson> people = Fakers.People.Generate(4);
        List<Relationship> relationships = [
            new(people[0], people[2], RelationshipType.DoNotMatch),
            new(people[0], people[3], RelationshipType.DoNotMatch),
        ];
        Generator systemUnderTest = new(settings, people, relationships);

        systemUnderTest.GenerateGroups();

        systemUnderTest.Groups.Should().HaveCount(2);
        systemUnderTest.Groups.Single(g => g.People.Contains(people[0])).People.Should().Contain(people[1]);
    }

    [Fact]
    public void GenerateGroups_WhenPeopleWantsToBeMatched()
    {
        GroupSettings settings = new()
        {
            GroupSize = 2,
            UseGroupSize = true,
        };
        List<IPerson> people = Fakers.People.Generate(4);
        List<Relationship> relationships = [
            new(people[0], people[1], RelationshipType.Match),
            new(people[1], people[0], RelationshipType.Match),
            new(people[2], people[1], RelationshipType.Match),
        ];
        Generator systemUnderTest = new(settings, people, relationships);

        systemUnderTest.GenerateGroups();

        systemUnderTest.Groups.Should().HaveCount(2);
        systemUnderTest.Groups.Single(g => g.People.Contains(people[0])).People.Should().Contain(people[1]);
    }
}