namespace Groupify.Core;

public class Generator(GroupSettings settings, List<IPerson> people, List<Relationship> relationships)
{
    public List<Group> Groups => groups;
    private readonly List<Group> groups = [];

    public void GenerateGroups()
    {
        if (!settings.ValidateSettings()) return;

        GenerateEmptyGroups();

        List<IPerson> clone = CopyList(people);

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
        for (int person1Index = group.People.Count - 1; person1Index >= 0; person1Index--)
        {
            var person1 = group.People[person1Index];
            int currentScore = CalculateGroupScore(group);

            foreach (Group group2 in groups)
            {
                if (group.Id == group2.Id)
                    continue;

                for (int person2Index = group2.People.Count - 1; person2Index >= 0; person2Index--)
                {
                    var person2 = group2.People[person2Index];
                    int group2CurrentScore = CalculateGroupScore(group2);
                    int updatedScore = CalculateGroupScoreIfReplaced(group, person1, person2) + CalculateGroupScoreIfReplaced(group2, person2, person1);

                    if (updatedScore < currentScore + group2CurrentScore)
                    {
                        SwapMembers(group, person1, group2, person2);
                    }
                }
            }
        }
    }

    private int CalculateGroupScore(Group group)
    {
        int score = group.MaxSize * group.MaxSize;
        foreach (IPerson member1 in group.People)
        {
            foreach (IPerson member2 in group.People.Where(p => p.Id != member1.Id))
            {
                IRelationshipType? relationshipType = null;
                if ((relationshipType = ContainsRelationship(member1, member2)) != null)
                {
                    score += relationshipType.Value;
                }
            }
        }

        return score;
    }

    private IRelationshipType? ContainsRelationship(IPerson p1, IPerson p2) => relationships.OrderBy(r => r.RelationshipType.Value).FirstOrDefault(r => r.Person1.Id == p1.Id && r.Person2.Id == p2.Id)?.RelationshipType;

    private int CalculateGroupScoreIfReplaced(Group group, IPerson personA, IPerson personB)
    {
        Group copy = group.Copy();
        copy.People.Remove(personA);
        copy.People.Add(personB);
        return CalculateGroupScore(copy);
    }

    private static void SwapMembers(Group group1, IPerson person1, Group group2, IPerson person2)
    {
        group1.People.Remove(person1);
        group1.People.Add(person2);
        group2.People.Remove(person2);
        group2.People.Add(person1);
    }

    private int GetIndexOfBestMatch(Group group, List<IPerson> people)
    {
        int[] points = new int[people.Count];

        for (int i = 0; i < people.Count; i++)
        {
            IPerson p = people[i];
            points[i] = group.MaxSize;

            foreach (IPerson person in group.People)
            {
                IRelationshipType? relationshipType = null;
                if ((relationshipType = ContainsRelationship(p, person)) != null)
                {
                    points[i] += relationshipType.Value;
                }
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

    private static List<IPerson> CopyList(List<IPerson> list)
    {
        List<IPerson> result = [.. list];
        return result;
    }
}
