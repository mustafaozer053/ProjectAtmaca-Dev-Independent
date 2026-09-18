using FluentAssertions;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.Persons.RegisterWithAtmacaCard;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;
using ProjectAtmaca.Domain.Persons.Registration;
using ProjectAtmaca.Domain.Services;

namespace ProjectAtmaca.Application.Tests.Persons.RegisterWithAtmacaCard;

public sealed class PreparePersonRegistrationTests
{
    [Fact]
    public async Task Prepare_Should_NotAllocate_WhenForbidden()
    {
        var numbers = new Numbers();
        var service = Create(numbers, allowed: false);
        var result = await service.PrepareAsync(Input(), TestContext.Current.CancellationToken);
        result.Error.Should().BeSameAs(ActorAuthorizationErrors.Forbidden);
        result.Value.Should().BeNull();
        numbers.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Prepare_Should_NotAllocate_WhenPersonOrIdentityInvalid(bool invalidPerson)
    {
        var numbers = new Numbers();
        var input = invalidPerson ? Input() with { Name = null! } : Input() with { NationalIdentityNumber = null };
        var result = await Create(numbers).PrepareAsync(input, TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        numbers.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(TurkishCitizenshipStatus.ByBirth)]
    [InlineData(TurkishCitizenshipStatus.Acquired)]
    [InlineData(TurkishCitizenshipStatus.NotTurkishCitizen)]
    public async Task Prepare_Should_LinkAllRecords_AndUseServerTime(TurkishCitizenshipStatus status)
    {
        var numbers = new Numbers();
        var date = status == TurkishCitizenshipStatus.Acquired ? new DateOnly(2020, 1, 1) : (DateOnly?)null;
        var input = Input() with
        {
            Status = status,
            NationalIdentityNumber = status == TurkishCitizenshipStatus.NotTurkishCitizen ? null : "12345678901",
            PassportNumber = " AB12345 ", TurkishCitizenshipAcquiredOn = date
        };
        var result = await Create(numbers).PrepareAsync(input, TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
        var draft = result.Value!;
        draft.Registration.PersonId.Should().Be(draft.Person.Id);
        draft.Card.PersonId.Should().Be(draft.Person.Id);
        draft.Registration.Identity.Status.Should().Be(status);
        draft.Registration.Identity.PassportNumber.Should().Be("AB12345");
        draft.Registration.Identity.TurkishCitizenshipAcquiredOn.Should().Be(date);
        draft.Card.CardNumber.Value.Should().Be("ATM-000001");
        draft.Card.IssuedAtUtc.Should().Be(FixedClock.Now.UtcDateTime);
        draft.Person.MotherName.Should().BeSameAs(input.MotherName);
        draft.Person.CreatedByActorId.Should().BeNull();
        numbers.Calls.Should().Be(1);
        numbers.Token.Should().Be(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Prepare_Should_ReturnFailure_WhenGeneratorReturnsInvalidNumber()
    {
        var result = await Create(new Numbers { Number = "invalid" }).PrepareAsync(Input(), TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_Should_ReturnCapacityError_WithoutReturningDraft()
    {
        var result = await Create(new Numbers { Exhausted = true }).PrepareAsync(Input(), TestContext.Current.CancellationToken);
        result.Error.Should().BeSameAs(PersonRegistrationOperationErrors.CardNumberCapacity);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_Should_LinkMultipleCitizenships_WithoutInventingUnknownDates()
    {
        var input = Input() with { Citizenships = [new(Country.Create("TR", "Türkiye")), new(Country.Create("FR", "France"), new DateOnly(2020, 1, 1))] };
        var result = await Create(new Numbers()).PrepareAsync(input, TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Citizenships.Should().HaveCount(2);
        result.Value.Citizenships.Should().OnlyContain(x => x.PersonId == result.Value.Person.Id);
        result.Value.Citizenships[0].AcquiredOn.Should().BeNull();
        result.Value.Citizenships[1].AcquiredOn.Should().Be(new DateOnly(2020, 1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Prepare_Should_RejectInvalidCitizenshipsBeforeNumberAllocation(int scenario)
    {
        var numbers = new Numbers();
        CitizenshipInput[] entries = scenario switch
        {
            0 => [new(null!)],
            1 => [new(Country.Create("FR", "France"), default(DateOnly))],
            _ => [new(Country.Create("FR", "France")), new(Country.Create("FR", "France"))]
        };
        var result = await Create(numbers).PrepareAsync(Input() with { Citizenships = entries }, TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        numbers.Calls.Should().Be(0);
    }

    [Fact]
    public void Replay_Should_IgnoreCitizenshipOrderButDetectDateChanges_AndSnapshotInputList()
    {
        var entries = new[] { new CitizenshipInput(Country.Create("TR", "Türkiye")), new CitizenshipInput(Country.Create("FR", "France")) };
        var input = Input() with { Citizenships = entries };
        var receipt = new RegistrationReceipt(Guid.NewGuid(), Guid.NewGuid(), "ATM-000001", DateTime.UtcNow);
        var completed = new CompletedPersonRegistration(input, receipt);
        completed.Matches(input with { Citizenships = entries.Reverse().ToArray() }).Should().BeTrue();
        entries[0] = entries[0] with { AcquiredOn = new DateOnly(2020, 1, 1) };
        input.Citizenships[0].AcquiredOn.Should().BeNull();
        completed.Matches(input with { Citizenships = entries }).Should().BeFalse();
    }

    [Theory]
    [InlineData(TurkishCitizenshipStatus.NotTurkishCitizen)]
    [InlineData(TurkishCitizenshipStatus.Acquired)]
    public async Task Prepare_Should_RejectContradictoryTurkishCitizenshipBeforeAllocation(TurkishCitizenshipStatus status)
    {
        var numbers = new Numbers();
        var input = Input() with
        {
            Status = status,
            PassportNumber = "AB12345",
            TurkishCitizenshipAcquiredOn = status == TurkishCitizenshipStatus.Acquired ? new DateOnly(2020, 1, 1) : null,
            Citizenships = [new(Country.Create("TR", "Türkiye"), new DateOnly(2021, 1, 1))]
        };
        var result = await Create(numbers).PrepareAsync(input, TestContext.Current.CancellationToken);
        result.Error.Should().BeSameAs(PersonRegistrationOperationErrors.CitizenshipIdentityConflict);
        result.Value.Should().BeNull();
        numbers.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Prepare_Should_AllowMissingOrMatchingSupplementaryTurkishDate(bool known)
    {
        var acquired = new DateOnly(2020, 1, 1);
        var input = Input() with
        {
            Status = TurkishCitizenshipStatus.Acquired, TurkishCitizenshipAcquiredOn = acquired,
            Citizenships = [new(Country.Create("TR", "Türkiye"), known ? acquired : null)]
        };
        var result = await Create(new Numbers()).PrepareAsync(input, TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Citizenships.Single().AcquiredOn.Should().Be(known ? acquired : null);
    }

    private static PersonRegistrationInput Input() => new(
        PersonName.Create("Test Person").Value!, BirthDate.Create(new DateTime(2010, 1, 1)),
        Country.Create("TR", "Türkiye"), TurkishCitizenshipStatus.ByBirth, "12345678901")
        { MotherName = PersonName.Create("Test Mother").Value! };

    private static PreparePersonRegistration Create(Numbers numbers, bool allowed = true) =>
        new(new PersonRegistrationAuthorization(new Authorization(allowed)), numbers, new FixedClock());

    private sealed class Authorization(bool allowed) : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(Permission permission, CancellationToken cancellationToken = default) =>
            Task.FromResult(allowed ? Result.Success() : Result.Failure(ActorAuthorizationErrors.Forbidden));
    }

    private sealed class Numbers : IAtmacaCardNumberGenerator
    {
        public bool Exhausted { get; init; }
        public string Number { get; init; } = "ATM-000001";
        public int Calls { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            Token = cancellationToken;
            if (Exhausted) throw new AtmacaCardNumberCapacityException(new InvalidOperationException("Capacity reached."));
            return Task.FromResult(Number);
        }
    }

    private sealed class FixedClock : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
