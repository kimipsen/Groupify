namespace Groupify.Core;
public class Generator(GroupSettings settings, List<Person> people, List<Relationship> relationships)
{
    public List<Group> Groups => groups;
    private readonly List<Group> groups = [];

    public void GenerateGroups()
    {
        if (!settings.ValidateSettings()) return;

        GenerateEmptyGroups();

        List<Person> clone = CopyList(people);

        // Add a single person to each group
        foreach (Group group in groups)
        {
            group.People.Add(clone[0]);
            clone.RemoveAt(0);
        }

        // Add remaining people to all groups, based on their preferences
        while (clone.Count != 0)
        {
            foreach (Group group in groups.Where(g => !g.IsFilled))
            {
                if (clone.Count == 0)
                    continue;

                int bestMatchIndex = GetIndexOfBestMatch(group, clone);
                group.People.Add(clone[bestMatchIndex]);
                clone.RemoveAt(bestMatchIndex);
            }
        }

        // Optimize groups
        for (int k = 0; k < groups.Count; k++)
        {
            ShouldOptimize(groups[k]);
        }
    }

    private void GenerateEmptyGroups()
    {
        if (settings.UseGroupSize)
        {
            int numberOfGroups = people.Count / settings.GroupSize;
            int remainder = people.Count % settings.GroupSize;

            for (int i = 0; i < numberOfGroups; i++)
            {
                if (i != numberOfGroups - 1)
                    groups.Add(new(i, settings.GroupSize));
                else
                    groups.Add(new(i, settings.GroupSize + remainder));
            }
        }
        else
        {
            int groupSize = people.Count / settings.NumberOfGroups;
            int remainder = people.Count % settings.NumberOfGroups;

            for (int i = 0; i < settings.NumberOfGroups; i++)
            {
                if (i != settings.NumberOfGroups - 1)
                    groups.Add(new(i, groupSize));
                else
                    groups.Add(new(i, groupSize + remainder));
            }
        }
    }

    private void ShouldOptimize(Group group)
    {
        foreach (Person person in group.People)
        {
            int currentScore = CalculateGroupScore(group);

            foreach (Group group2 in groups)
            {
                if (group.Id == group2.Id)
                    continue;

                foreach (Person person2 in group2.People)
                {
                    int group2CurrentScore = CalculateGroupScore(group2);
                    int updatedScore = CalculateGroupScoreIfReplaced(group, person, person2) + CalculateGroupScoreIfReplaced(group2, person2, person);

                    if (updatedScore < currentScore + group2CurrentScore)
                    {
                        SwapMembers(group, person, group2, person2);
                    }
                }
            }
        }
    }

    private int CalculateGroupScore(Group group)
    {
        int score = group.MaxSize * group.MaxSize;
        foreach (Person member1 in group.People)
        {
            foreach (Person member2 in group.People.Where(p => p.Id != member1.Id))
            {
                if (ContainsRelationship(member1, member2, RelationshipType.Match))
                    score += (int)RelationshipType.Match;

                if (ContainsRelationship(member1, member2, RelationshipType.DoNotMatch))
                    score += (int)RelationshipType.Match;
            }
        }

        return score;
    }

    private bool ContainsRelationship(Person p1, Person p2, RelationshipType relationshipType) => relationships.Exists(r => r.Person1.Id == p1.Id && r.Person2.Id == p2.Id && r.RelationshipType == relationshipType);

    private int CalculateGroupScoreIfReplaced(Group group, Person person1, Person person2)
    {
        Group copy = group.Copy();
        copy.People.Remove(person1);
        copy.People.Add(person2);
        return CalculateGroupScore(copy);
    }

    private void SwapMembers(Group group1, Person person1, Group group2, Person person2)
    {
        group1.People.Remove(person1);
        group1.People.Add(person2);
        group2.People.Remove(person2);
        group2.People.Add(person1);
    }

    private int GetIndexOfBestMatch(Group group, List<Person> people)
    {
        int[] points = new int[people.Count];

        for (int i = 0; i < people.Count; i++)
        {
            Person p = people[i];
            points[i] = group.MaxSize;

            foreach (Person person in group.People)
            {
                if (ContainsRelationship(p, person, RelationshipType.Match))
                    points[i] += (int)RelationshipType.Match;

                if (ContainsRelationship(p, person, RelationshipType.DoNotMatch))
                    points[i] += (int)RelationshipType.DoNotMatch;
            }
        }

        int minIndex = 0;
        for (int j = 0; j < points.Length; j++)
        {
            if (points[j] < points[minIndex])
                minIndex = j;
        }
        return minIndex;
    }

    private List<Person> CopyList(List<Person> list)
    {
        List<Person> result = new(list.Count);
        foreach (Person person in list)
        {
            result.Add(new(person.Id, person.FirstName, person.LastName));
        }
        return result;
    }
}
