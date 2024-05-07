using Bogus;

namespace Groupify.Core.Test;
internal static class Fakers
{
    private const int PersonSeed = 2345667;

    private static readonly Faker<IPerson> PersonFaker = new Faker<IPerson>()
        .UseSeed(PersonSeed)
        .CustomInstantiator(f => new Person(f.IndexFaker, f.Person.FirstName, f.Person.LastName))
    ;

    public static Faker<IPerson> People = PersonFaker;

    public record class Person(int Id, string FirstName, string LastName) : IPerson;
}