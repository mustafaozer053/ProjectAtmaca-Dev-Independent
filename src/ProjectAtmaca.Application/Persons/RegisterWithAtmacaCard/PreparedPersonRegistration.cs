using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Persons;
using ProjectAtmaca.Domain.Persons.Registration;

namespace ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;

/// <summary>In-memory records only. This result does not indicate a committed registration.</summary>
public sealed class PreparedPersonRegistration
{
    public Person Person { get; }
    public PersonRegistration Registration { get; }
    public AtmacaCard Card { get; }
    public IReadOnlyList<PersonCitizenship> Citizenships { get; }

    internal PreparedPersonRegistration(Person person, PersonRegistration registration, AtmacaCard card,
        IEnumerable<PersonCitizenship> citizenships)
    {
        Person = person;
        Registration = registration;
        Card = card;
        Citizenships = Array.AsReadOnly(citizenships.ToArray());
    }
}
