using Bogus;

namespace Groupify.Core.Test;
internal static class Fakers
{
    private const int PersonSeed = 2345667;

    private static readonly Faker<Person> PersonFaker = new Faker<Person>()
        .UseSeed(PersonSeed)
        .CustomInstantiator(f => new(f.IndexFaker, f.Person.FirstName, f.Person.LastName))
    ;

    public static Faker<Person> People = PersonFaker;
}