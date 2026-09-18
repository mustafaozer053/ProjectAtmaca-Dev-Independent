using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Domain.Services;
using ProjectAtmaca.Infrastructure.Persistence.Persons;
using ProjectAtmaca.Infrastructure.Tests.Persistence.Participations;

namespace ProjectAtmaca.Infrastructure.Tests.Persistence.Persons;

[Collection(SqlIntegrationCollection.Name)]
public sealed class PersonRegistrationSqlTests
{
    private static int _number = 700000;
    private static CancellationToken Token => CancellationToken.None;

    [Fact]
    public async Task Commit_Should_RoundTripAllRecordsAndReplayInput_FromFreshContext()
    {
        var actor = ActorId.New();
        var operation = Guid.NewGuid();
        var input = Input() with
        {
            BirthCountry = Country.Create("TR", "Türkiye"),
            Citizenships = [new(Country.Create("FR", "France")), new(Country.Create("DE", "Germany"), new DateOnly(2020, 1, 1))],
            MotherName = PersonName.Create("Test Mother").Value!,
            BirthPlace = Location.Create(Country.Create("TR", "Türkiye"), "Rize", "Merkez"),
            Email = Email.Create("test@example.com"), PrimaryPhoneNumber = PhoneNumber.Create("90", "5551234567"),
            Address = Address.Create(Location.Create(Country.Create("TR", "Türkiye"), "Rize"), "Test address", "53000")
        };
        var draft = await Prepare(input);
        await Commit(actor, operation, input, draft);

        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        var person = await context.Set<Person>().SingleAsync(x => x.Id == draft.Person.Id, Token);
        person.Name.Should().Be(input.Name);
        person.BirthPlace.Should().Be(input.BirthPlace);
        person.MotherName.Should().Be(input.MotherName);
        person.FatherName.Should().BeNull();
        person.Address.Should().Be(input.Address);
        person.Email.Should().Be(input.Email);
        person.PrimaryPhoneNumber.Should().Be(input.PrimaryPhoneNumber);
        person.CreatedByActorId.Should().Be(actor);
        var citizenships = await context.Set<PersonCitizenship>().Where(x => x.PersonId == person.Id).ToListAsync(Token);
        citizenships.Should().HaveCount(2);
        citizenships.Should().OnlyContain(x => x.CreatedByActorId == actor);
        citizenships.Single(x => x.Country.Code == "FR").AcquiredOn.Should().BeNull();
        citizenships.Single(x => x.Country.Code == "DE").AcquiredOn.Should().Be(new DateOnly(2020, 1, 1));
        var registration = await context.Set<PersonRegistration>().SingleAsync(x => x.PersonId == person.Id, Token);
        registration.Identity.PassportNumber.Should().Be("AB12345");
        registration.CreatedByActorId.Should().Be(actor);
        var card = await context.Set<AtmacaCard>().SingleAsync(x => x.PersonId == person.Id, Token);
        card.IssuedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        card.CreatedByActorId.Should().Be(actor);
        var previous = await new SqlPersonRegistrationStore(context).FindAsync(actor, operation, Token);
        previous!.Matches(input).Should().BeTrue();
        previous.Receipt.PersonId.Should().Be(person.Id);
        (await new SqlPersonRegistrationStore(context).FindAsync(ActorId.New(), operation, Token)).Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentExactReplay_Should_CommitOnlyOneDraft()
    {
        var actor = ActorId.New();
        var operation = Guid.NewGuid();
        var input = Input();
        var first = await Prepare(input);
        var second = await Prepare(input);
        var results = await Task.WhenAll(Commit(actor, operation, input, first), Commit(actor, operation, input, second));
        results.Should().OnlyContain(x => x.IsSuccess);
        results[0].Value.Should().Be(results[1].Value);
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        var ids = new[] { first.Person.Id, second.Person.Id };
        (await context.Set<Person>().CountAsync(x => ids.Contains(x.Id), Token)).Should().Be(1);
        (await context.Set<AtmacaCard>().CountAsync(x => ids.Contains(x.PersonId), Token)).Should().Be(1);
        (await context.Set<PersonRegistration>().CountAsync(x => ids.Contains(x.PersonId), Token)).Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Commit_Should_PreserveTurkishIdentityAndOptionalAcquisitionDate(bool acquired)
    {
        var actor = ActorId.New();
        var operation = Guid.NewGuid();
        var date = acquired ? new DateOnly(2020, 6, 12) : (DateOnly?)null;
        var input = Input() with
        {
            Status = acquired ? TurkishCitizenshipStatus.Acquired : TurkishCitizenshipStatus.ByBirth,
            NationalIdentityNumber = acquired ? "12345678903" : "12345678902", PassportNumber = null, TurkishCitizenshipAcquiredOn = date
        };
        var draft = await Prepare(input);
        await Commit(actor, operation, input, draft);
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        var registration = await context.Set<PersonRegistration>().SingleAsync(x => x.PersonId == draft.Person.Id, Token);
        registration.Identity.Status.Should().Be(input.Status);
        registration.Identity.NationalIdentityNumber.Should().Be(input.NationalIdentityNumber);
        registration.Identity.TurkishCitizenshipAcquiredOn.Should().Be(date);
        registration.Identity.PassportNumber.Should().BeNull();
        (await new SqlPersonRegistrationStore(context).FindAsync(actor, operation, Token))!.Matches(input).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentDifferentInput_Should_ReturnOneConflict()
    {
        var actor = ActorId.New();
        var operation = Guid.NewGuid();
        var firstInput = Input();
        var secondInput = Input() with { PassportNumber = "DIFFERENT" };
        var first = await Prepare(firstInput);
        var second = await Prepare(secondInput);
        var results = await Task.WhenAll(Commit(actor, operation, firstInput, first), Commit(actor, operation, secondInput, second));
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Single(x => x.IsFailure).Error.Should().BeSameAs(PersonRegistrationOperationErrors.OperationConflict);
    }

    [Fact]
    public async Task DuplicateCardNumber_Should_RollBackPersonRegistrationAndOperation()
    {
        var actor = ActorId.New();
        var input = Input();
        var first = await Prepare(input);
        await Commit(actor, Guid.NewGuid(), input, first);
        input = input with { Citizenships = [new(Country.Create("FR", "France"))] };
        var second = await Prepare(input, first.Card.CardNumber.Value);
        var operation = Guid.NewGuid();
        Func<Task> failing = async () => await Commit(actor, operation, input, second);
        await failing.Should().ThrowAsync<DbUpdateException>();
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        (await context.Set<Person>().AnyAsync(x => x.Id == second.Person.Id, Token)).Should().BeFalse();
        (await context.Set<PersonRegistration>().AnyAsync(x => x.PersonId == second.Person.Id, Token)).Should().BeFalse();
        (await context.Set<PersonCitizenship>().AnyAsync(x => x.PersonId == second.Person.Id, Token)).Should().BeFalse();
        (await context.Set<AtmacaCard>().AnyAsync(x => x.Id == second.Card.Id, Token)).Should().BeFalse();
        (await new SqlPersonRegistrationStore(context).FindAsync(actor, operation, Token)).Should().BeNull();
    }

    [Fact]
    public async Task DifferentActorsAndOperations_Should_NotCreateSameNationalIdentityTwice()
    {
        var input = Input() with { Status = TurkishCitizenshipStatus.ByBirth, NationalIdentityNumber = "12345678904" };
        var first = await Prepare(input);
        var second = await Prepare(input);
        var results = await Task.WhenAll(
            Commit(ActorId.New(), Guid.NewGuid(), input, first),
            Commit(ActorId.New(), Guid.NewGuid(), input, second));
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Single(x => x.IsFailure).Error.Should().BeSameAs(PersonRegistrationOperationErrors.IdentityAlreadyRegistered);
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        var ids = new[] { first.Person.Id, second.Person.Id };
        (await context.Set<Person>().CountAsync(x => ids.Contains(x.Id), Token)).Should().Be(1);
        (await context.Set<AtmacaCard>().CountAsync(x => ids.Contains(x.PersonId), Token)).Should().Be(1);
    }

    [Fact]
    public async Task PassportDuplicate_Should_RequireConfirmationAndReason_AndRetainEvidence()
    {
        var input = Input() with { PassportNumber = Guid.NewGuid().ToString("N"), ConfirmPossiblePassportDuplicate = false, PassportDuplicateReason = null };
        var original = await Prepare(input);
        await Commit(ActorId.New(), Guid.NewGuid(), input, original);
        var actor = ActorId.New();
        var operation = Guid.NewGuid();
        var draft = await Prepare(input);
        (await Commit(actor, operation, input, draft)).Error.Should().BeSameAs(PersonRegistrationOperationErrors.PassportDuplicateConfirmationRequired);
        var confirmed = input with { ConfirmPossiblePassportDuplicate = true };
        (await Commit(actor, operation, confirmed, draft)).Error.Should().BeSameAs(PersonRegistrationOperationErrors.PassportDuplicateReasonRequired);
        await using (var context = ParticipationPersistenceTestContextFactory.CreateContext())
        {
            (await context.Set<Person>().AnyAsync(x => x.Id == draft.Person.Id, Token)).Should().BeFalse();
            (await new SqlPersonRegistrationStore(context).FindAsync(actor, operation, Token)).Should().BeNull();
        }
        confirmed = confirmed with { PassportDuplicateReason = "Different person; document issuing country pending." };
        var result = await Commit(actor, operation, confirmed, draft);
        result.IsSuccess.Should().BeTrue();
        result.Value!.PersonId.Should().NotBe(original.Person.Id);
        await using var read = ParticipationPersistenceTestContextFactory.CreateContext();
        var saved = await new SqlPersonRegistrationStore(read).FindAsync(actor, operation, Token);
        saved!.Input.ConfirmPossiblePassportDuplicate.Should().BeTrue();
        saved.Input.PassportDuplicateReason.Should().Be(confirmed.PassportDuplicateReason);
        var replay = await Commit(actor, operation, confirmed, draft);
        replay.Value.Should().Be(result.Value);
        (await Commit(actor, operation, confirmed with { PassportDuplicateReason = "Changed reason" }, draft)).Error
            .Should().BeSameAs(PersonRegistrationOperationErrors.OperationConflict);
    }

    [Fact]
    public async Task ConcurrentUnconfirmedPassportMatch_Should_RequireConfirmationForOneRequest()
    {
        var input = Input() with { PassportNumber = Guid.NewGuid().ToString("N"), ConfirmPossiblePassportDuplicate = false, PassportDuplicateReason = null };
        var first = await Prepare(input);
        var second = await Prepare(input);
        var results = await Task.WhenAll(Commit(ActorId.New(), Guid.NewGuid(), input, first), Commit(ActorId.New(), Guid.NewGuid(), input, second));
        results.Count(x => x.IsSuccess).Should().Be(1);
        results.Single(x => x.IsFailure).Error.Should().BeSameAs(PersonRegistrationOperationErrors.PassportDuplicateConfirmationRequired);
    }

    private static PersonRegistrationInput Input() => new(PersonName.Create("SQL Test Person").Value!,
        BirthDate.Create(new DateTime(2010, 1, 1)), Country.Create("FR", "France"),
        TurkishCitizenshipStatus.NotTurkishCitizen, PassportNumber: "AB12345")
        { ConfirmPossiblePassportDuplicate = true, PassportDuplicateReason = "Separate synthetic fixture person." };

    private static async Task<PreparedPersonRegistration> Prepare(PersonRegistrationInput input, string? number = null)
    {
        var result = await new PreparePersonRegistration(new PersonRegistrationAuthorization(new Allow()),
            new Numbers(number ?? $"ATM-{Interlocked.Increment(ref _number):D6}"), TimeProvider.System).PrepareAsync(input, Token);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }
    private static async Task<Result<RegistrationReceipt>> Commit(ActorId actor, Guid operation,
        PersonRegistrationInput input, PreparedPersonRegistration draft)
    {
        await using var context = ParticipationPersistenceTestContextFactory.CreateContext();
        return await new SqlPersonRegistrationStore(context).CommitAsync(actor, operation, input, draft, Token);
    }
    private sealed class Allow : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
    }
    private sealed class Numbers(string number) : IAtmacaCardNumberGenerator
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default) => Task.FromResult(number);
    }
}
